
using Fujitsu.eDoc.SAPA.WebService.Services;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Text;
using System.Xml.Linq;
using System.Linq;
using log4net;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace Fujitsu.eDoc.SAPA.WebService.Controllers
{
    public class MessageDistributorController : ApiController
    {
        private XDocument doc = new XDocument();
        private string MessageID { get; set; }
        private HttpResponseMessage httpResponseMessage;
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        [System.Web.Mvc.HttpPost]
        public HttpResponseMessage Post(HttpRequestMessage request)
        {
            try
            {
                string contentResult = string.Empty;
                ValidateRequest(ref contentResult, request);

                using (StringReader s = new StringReader(contentResult))
                {
                    doc = XDocument.Load(s);
                }

                MessageID = ExtractMsgID();

                string path = MessagePath;

                //Directory creation
                DirectoryInfo dir = Directory.CreateDirectory($@"{path}\PostRequests");

                string dirName = dir.Name;

                //Format file name
                var uniqueFileName = UniqueFileNameFormat();

                doc.Save($@"{path}\{dirName}\{uniqueFileName}");

                httpResponseMessage = new HttpResponseMessage
                {
                    Content = new StringContent(CreateStandardReturType(), Encoding.UTF8, "application/xml"),
                    StatusCode = HttpStatusCode.OK,
                };

                log.Info($"The message id: {MessageID} was sucessfully received by the POST request from the messagedistributor.");

            }

            catch (Exception e)
            {
               log.Error($"Error occured on the messsage id: {MessageID} when received a POST request from the messagedistributor: {e}");

                httpResponseMessage = new HttpResponseMessage
                {
                    Content = new StringContent(CreateStandardBadReturType(), Encoding.UTF8, "application/xml"),
                    StatusCode = HttpStatusCode.BadRequest,
                };
            }

            return httpResponseMessage;
        }

        private string ExtractMsgID()
        {
            var msg = from n in doc.Descendants()
                      where n.Name.LocalName == "BeskedId"
                      select n;

            return msg.First().Value;
        }

        private string CreateStandardReturType()
        {
            return @"<ns2:ModtagBeskedOutput xmlns=""urn:oio:sagdok:3.0.0"" xmlns:ns2=""urn:oio:sts:1.0.0"">
                        <StandardRetur>
                            <StatusKode>20</StatusKode>
                            <FejlbeskedTekst>OK</FejlbeskedTekst>
                        </StandardRetur>
                    </ns2:ModtagBeskedOutput>";
        }


        private string CreateStandardBadReturType()
        {
            return @"<ns2:ModtagBeskedOutput xmlns=""urn:oio:sagdok:3.0.0"" xmlns:ns2=""urn:oio:sts:1.0.0"">
                    <StandardRetur>
                        <StatusKode>40</StatusKode>
                        <FejlbeskedTekst>Besked kan ikke modtages</FejlbeskedTekst>
                    </StandardRetur>
                  </ns2:ModtagBeskedOutput>";
        }

        private void ValidateRequest(ref string contentResult, HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("HttpRequestMessage is null");
            }

            if (request.Content == null)
            {
                throw new ArgumentNullException("HttpRequestMessage.Content is null");
            }

            if (request.Content == null)
            {
                throw new ArgumentNullException("HttpRequestMessage.Content is null");
            }

            contentResult = request.Content.ReadAsStringAsync().Result;
            if (string.IsNullOrEmpty(contentResult))
            {
                throw new ArgumentNullException("HttpRequestMessage.Content has empty content");
            }
        }

        private string MessagePath
        {
            get
            {
                if (String.IsNullOrEmpty(ConfigurationManager.AppSettings["Fujitsu.eDoc.SAPA.MessageDistribution.FilePath"]))
                {
                    throw new ConfigurationErrorsException("Missing Fujitsu.eDoc.SAPA.MessageDistribution.FilePath in config");
                }

                return ConfigurationManager.AppSettings["Fujitsu.eDoc.SAPA.MessageDistribution.FilePath"].ToString();
            }
        }


        private string UniqueFileNameFormat()
        {
            return string.Format(@"{0}.xml", DateTime.Now.ToString("yyyy_ddMM_HHmm_ss"));
        }

    }

}