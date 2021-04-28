using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Diagnostics;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace FileUtils.Code
{

    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public class FileUtilsService : IFileUtilsService
    {
        public string authorizationCode;

        string srcSite = "http://sps2019/sites/Test";
        string srcWeb = "/";
        string srcList = "";
        string allowedGroup = string.Empty;
        string currentUserEmail = string.Empty;

        private PdfSignatureAppearance appearance = null;
        string errorMessage = string.Empty;
        string userName = "Bogdan Bucur";
        int csize = 8192;
        MemoryStream ms = new MemoryStream();

        private MemoryStream serializeResponse(object response)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(new JavaScriptSerializer().Serialize(response)));
        }

        public static string BytesToString(byte[] bytes)
        {
            string result = "";
            foreach (byte b in bytes) result += b.ToString("x2");
            return result;
        }

        public Stream GetHashPDF(string idFile)
        {
            using (SPSite site = new SPSite("http://localhost:8080/"))
            {
                using (SPWeb web = site.RootWeb)
                {
                    web.AllowUnsafeUpdates = true;
                    currentUserEmail = web.CurrentUser.Name;
                    SPList listFiles = web.Lists["Documents"];

                    try
                    {
                        SPListItem selectedItem = listFiles.GetItemById(Int32.Parse(idFile));
                        // Stream inputStream = new MemoryStream(selectedItem.File.OpenBinary());

                        CreateITextAppearance(selectedItem.File.OpenBinary());
                        string hash = generateHash();
                        return serializeResponse(
                          new
                          {
                              Result = OperationResult.Success,
                              Message = hash
                          }
                           );
                    }
                    catch (Exception e)
                    {
                        return serializeResponse(
                         new
                         {
                             Result = OperationResult.Error,
                             Message = $"Eroare semnare! {e.Message}"
                         }
                        );
                    }
                }
            }


        }
        //schimbare functie pentru iText
        
        private string generateHash()
        {
            appearance.SetVisibleSignature(new Rectangle(500, 150, 400, 200), 1, "signature");
            appearance.SignDate = DateTime.Now;
            appearance.Reason = "Test Licenta";
            appearance.Location = "Bucuresti";
            appearance.Contact = "mta";
            StringBuilder buf = new StringBuilder();
            buf.Append("Semnat digital de");
            buf.Append("\n");
            buf.Append(userName);
            buf.Append("\n");
            buf.Append("Date: " + appearance.SignDate);
            appearance.Layer2Text = buf.ToString();
            appearance.Acro6Layers = true;
            appearance.CertificationLevel = 0;
            PdfSignature dic = new PdfSignature(PdfName.ADOBE_PPKLITE, PdfName.ADBE_PKCS7_DETACHED)
            {
                Date = new PdfDate(appearance.SignDate),
                Name = userName
            };
            dic.Reason = appearance.Reason;
            dic.Location = appearance.Location;
            dic.Contact = appearance.Contact;

            appearance.CryptoDictionary = dic;
            Dictionary<PdfName, int> exclusionSizes = new Dictionary<PdfName, int>();
            exclusionSizes.Add(PdfName.CONTENTS, (csize * 2) + 2);
            appearance.PreClose(exclusionSizes);

            HashAlgorithm sha = new SHA256CryptoServiceProvider();
            Stream s = appearance.GetRangeStream();
            int read = 0;
            byte[] buff = new byte[0x2000];
            while ((read = s.Read(buff, 0, 0x2000)) > 0)
            {
                sha.TransformBlock(buff, 0, read, buff, 0);
            }
            sha.TransformFinalBlock(buff, 0, 0);
       
            return System.Convert.ToBase64String(sha.Hash);
        }

        
        void CreateITextAppearance(byte[] Content)
        {
            var content = Content;
            var reader = new PdfReader(content);
            ms = new MemoryStream();
            var stamper = PdfStamper.CreateSignature(reader, ms, '\0');
            appearance = stamper.SignatureAppearance;
        }



        public Stream SignPDF(string bearer, string hash, string id, string SAD)
        {

            var resultSADToken = CallWebServiceSADToken(SAD).GetAwaiter().GetResult();
            JObject parseT = JObject.Parse(resultSADToken);
            if (parseT.Properties().First().Name.ToString() != "access_token")
            {
                return serializeResponse(
                new
                {
                    //operation failed
                    Result = OperationResult.Error,
                    //tratare eroare credentiale + cum vine 
                    Message = $"Eroare obtinere SAD ! {parseT.Properties().First().Value.ToString()}"
                }
                );
            }
            var newSAD = parseT.Properties().First().Value.ToString();
            var result = CallWebServiceSginPDF(bearer, hash, newSAD).GetAwaiter().GetResult();
            JObject o = JObject.Parse(result);
            string raspuns = o.Properties().First().Name.ToString();
            if (raspuns != "signatures")
            {
                return serializeResponse(
                new
                {
                    //operation failed
                    Result = OperationResult.Error,
                    //tratare eroare credentiale + cum vine 
                    Message = $"Eroare semnare! {o.Properties().First().Value.ToString()}"
                }
                );
            }
            string hashSign = o.Properties().First().Value[0].ToString();
            //hashSign = hashSign.Substring(1, hashSign.Length - 1);
            EmbededHashInPDF(Convert.FromBase64String(hashSign), id);
            Console.WriteLine(hashSign);
            return serializeResponse(
            new
            {
                Result = OperationResult.Success,
                Message = $"{result}"
            });
        }

        //ia semnatura de al serviciu si o baga in fisier
        private void EmbededHashInPDF(byte[] pk, string idFile)
        {
            //return ms.ToArray();
            using (SPSite site = new SPSite("http://localhost:8080/"))
            {
                using (SPWeb web = site.RootWeb)
                {
                    web.AllowUnsafeUpdates = true;
                    currentUserEmail = web.CurrentUser.Name;
                    SPList listFiles = web.Lists["Documents"];

                    try
                    {
                        SPListItem selectedItem = listFiles.GetItemById(Int32.Parse(idFile));
                        // Stream inputStream = new MemoryStream(selectedItem.File.OpenBinary());

                        CreateITextAppearance(selectedItem.File.OpenBinary());
                        string hash = generateHash();

                        byte[] paddedSig = new byte[csize];
                        System.Array.Copy(pk, 0, paddedSig, 0, pk.Length);

                        PdfDictionary dic2 = new PdfDictionary();
                        dic2.Put(PdfName.CONTENTS, new PdfString(paddedSig).SetHexWriting(true));
                        appearance.Close(dic2);

                        //System.IO.File.WriteAllBytes(@"C:\PDF\out.pdf", ms.ToArray());

                        selectedItem.File.ParentFolder.Files.Add(selectedItem.Name, ms.ToArray(), true);
                        //SPListItem currentFile = listFiles.GetItemById(Int32.Parse(publicFileID));

                    }
                    catch (Exception e)
                    {

                    }
                }
            }


        }


        public Stream GetAccessToken(string code)
        {
            var result = CallWebService(code).GetAwaiter().GetResult();
            return serializeResponse(
            new
            {
                Result = OperationResult.Success,
                Message = $"{result}"

            });
        }

        public async Task<string> CallWebService(string code)
        {
            try
            {
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json, charset=utf-8");
                //access_token pentru SAD final
                object data = new
                {
                    grant_type = "authorization_code",
                    code = code,
                    client_id = "msdiverse",
                    client_secret = "8KKhHnjKdYmAakc8",
                    redirect_uri = "http://localhost:8080/"
                };

                var myContent = JsonConvert.SerializeObject(data);

                var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);

                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.PostAsync("https://msign-test.transsped.ro/csc/v0/oauth2/token", byteContent);
                var responsestring = await response.Content.ReadAsStringAsync();

                return responsestring;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }


        public async Task<string> CallWebServiceSADToken(string codeSad)
        {
            try
            {
                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json, charset=utf-8");

                object data = new
                {
                    grant_type = "authorization_code",
                    code = codeSad,
                    client_id = "msdiverse",
                    client_secret = "8KKhHnjKdYmAakc8",
                    redirect_uri = "http://localhost:8080/"
                };

                var myContent = JsonConvert.SerializeObject(data);

                var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);

                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.PostAsync("https://msign-test.transsped.ro/csc/v0/oauth2/token", byteContent);
                var responsestring = await response.Content.ReadAsStringAsync();

                return responsestring;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }


        public async Task<string> CallWebServiceSginPDF(string bearer, string hash, string sadToken)
        {
            try
            {
                HttpClient client = new HttpClient();

                client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json, charset=utf-8");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearer);
                object data = new
                {
                    credentialID = "A122E0EFAF8C75AE0B3091183E9641AAD70C97DF",
                    SAD = sadToken,
                    hash = new string[] { hash },
                    hashAlgo = "2.16.840.1.101.3.4.2.1",
                    signAlgo = "1.2.840.113549.1.1.1",
                    clientData = "12345678"
                };

                var myContent = JsonConvert.SerializeObject(data);

                var buffer = System.Text.Encoding.UTF8.GetBytes(myContent);
                var byteContent = new ByteArrayContent(buffer);

                byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var response = await client.PostAsync("https://msign-test.transsped.ro/csc/v0/signatures/signHash", byteContent);
                var responsestring = await response.Content.ReadAsStringAsync();

                return responsestring;
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }


    }

    public enum OperationResult
    {
        Success,
        Warning,
        Error
    }
    public enum EventType
    {
        Info,
        Warning,
        Error

    }

}



