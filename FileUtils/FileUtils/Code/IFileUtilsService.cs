using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;

namespace FileUtils.Code
{
    [ServiceContract]
    public interface IFileUtilsService
    {

        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/SignPDF/{fileID}/{listID}/{webID}/{siteID}/{code}")]
        Stream SignPDF(string fileID, string listID, string webID, string siteID, string code);
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/GetAccessToken/{code}")]
        Stream GetAccessToken(string code);
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/GetCodeForSign/{credentials}/{hashPDF}/{accessToken}")]
        Stream GetCodeForSign(string credentials, string hashPDF, string accessToken);


    }
}
