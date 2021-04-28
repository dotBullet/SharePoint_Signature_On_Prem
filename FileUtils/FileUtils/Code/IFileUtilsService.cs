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

        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/SignPDF/{bearer}/{hash}/{id}/{SAD}")]
        Stream SignPDF(string bearer, string hash, string id, string SAD);
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/GetAccessToken/{code}")]
        Stream GetAccessToken(string code);
        [WebInvoke(Method = "GET", RequestFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedRequest, UriTemplate = "/GetHashPDF/{idFile}")]
        Stream GetHashPDF(string idFile);




    }
}
