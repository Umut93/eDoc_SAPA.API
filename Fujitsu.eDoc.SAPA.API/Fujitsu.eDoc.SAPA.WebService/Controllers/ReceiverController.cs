using Fujitsu.eDoc.SAPA.Common.Models;
using Fujitsu.eDoc.SAPA.WebService.Services;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Results;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Fujitsu.eDoc.SAPA.WebService.Controllers
{
    public class ReceiverController : ApiController
    {
        #region Private fields
        private string JournalNoteFilePath
        {
            get
            {
                if (ConfigurationManager.AppSettings["Fujitsu.eDoc.SAPA.JournalNote.FilePath"] == null)
                {
                    throw new ConfigurationErrorsException("Missing Fujitsu.eDoc.SAPA.JournalNote.FilePath in config");
                }

                return ConfigurationManager.AppSettings["Fujitsu.eDoc.SAPA.JournalNote.FilePath"].ToString();
            }
        }

        private const string _targetNamespace = "http://serviceplatformen.dk/xml/wsdl/soap11/DistributionService/3/types";
        private const string _xsdPath = @"\XML\DistributionServiceMsg.xsd";
        #endregion

        #region Public methods
        [HttpPost]
        public FordelingsobjektModtagResponse ReceiveJournalNote(HttpRequestMessage request)
        {            
            string fullFilePathSaved = string.Empty;
            try
            {
                string contentResult = string.Empty;
                FordelingsobjektAfsendRequest fordelingsobjektAfsendRequest = ValidateRequest(ref contentResult, request);
                fullFilePathSaved = SaveXmlToFile(contentResult, fordelingsobjektAfsendRequest);
            }
            catch (Exception ex)
            {
                LogService.LogToEventLog($"Error receiving journal note from SAPA: " + ex.ToString(), System.Diagnostics.EventLogEntryType.Error);

                string message = string.Empty;
                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;
                    message += ex.Message;
                }

                return CreateFordelingsobjektModtagResponseRejected(ex);
            }

            LogService.LogToEventLog(
                $" - Journalnote request successfully received and saved" +
                $"{Environment.NewLine} - XML-file located here: {fullFilePathSaved}" +
                $"{Environment.NewLine} - Sending technical receipt to ServicePlatformen now"
                , System.Diagnostics.EventLogEntryType.Information);       
            return CreateFordelingsobjektModtagResponseReceived();
        }

        [HttpGet]
        public IHttpActionResult Index()
        {
            return new ResponseMessageResult(Request.CreateResponse((HttpStatusCode)200, "Receiver is ready"));
        }
        #endregion

        #region Private methods
        private string SaveXmlToFile(string content, FordelingsobjektAfsendRequest fordelingsobjektAfsendRequest)
        {
            if (fordelingsobjektAfsendRequest == null)
            {
                throw new ArgumentException("Missing FordelingsobjektAfsendRequestType in XML");
            }
            
            string municipalityCVR = fordelingsobjektAfsendRequest.AuthorityContext.MunicipalityCVR.ToString();

            // This will create directory if it does not exist
            string fullPath = JournalNoteFilePath + "\\" + municipalityCVR;
            Directory.CreateDirectory(fullPath);
            string fullFilePath = fullPath + "\\TempJournalNoteXML-" + DateTime.Now.Ticks + ".xml";

            File.WriteAllText(fullFilePath, content);

            return fullFilePath;
        }

        private FordelingsobjektAfsendRequest ValidateRequest(ref string contentResult, HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("HttpRequestMessage is null");
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

            ValidateAgainstXSD(contentResult);

            FordelingsobjektAfsendRequest fordelingsobjektAfsendRequest = null;
            XmlSerializer serializer = new XmlSerializer(typeof(FordelingsobjektAfsendRequest));
            using (var reader = XmlReader.Create(new StringReader(contentResult)))
            {
                fordelingsobjektAfsendRequest = (FordelingsobjektAfsendRequest)serializer.Deserialize(reader);
            }

            if (fordelingsobjektAfsendRequest == null)
            {
                throw new ArgumentException("Missing FordelingsobjektAfsendRequestType in XML");
            }
            if (fordelingsobjektAfsendRequest.anmodning == null)
            {
                throw new ArgumentException("Missing FordelingsobjektAfsendRequestTyp.anmodning in XML");
            }
            if (fordelingsobjektAfsendRequest.anmodning.DistributionObject == null)
            {
                throw new ArgumentException("Missing FordelingsobjektAfsendRequestType.anmodning.DistributionObject in XML");
            }
            if (fordelingsobjektAfsendRequest.anmodning.DistributionObject.ObjektIndhold == null)
            {
                throw new ArgumentException("Missing FordelingsobjektAfsendRequestType.anmodning.DistributionObject.ObjektIndhold in XML");
            }
            if (fordelingsobjektAfsendRequest.anmodning.DistributionObject.ObjektIndhold.DistributionJournalPost == null)
            {
                throw new ArgumentException("Missing FordelingsobjektAfsendRequestType.anmodning.DistributionObject.ObjektIndhold.DistributionJournalPost in XML");
            }
            if (fordelingsobjektAfsendRequest.anmodning.DistributionObject.ObjektIndhold.DistributionJournalPost.SagForslag == null)
            {
                throw new ArgumentException("Missing FordelingsobjektAfsendRequestType.anmodning.DistributionObject.ObjektIndhold.DistributionJournalPost.SagForslag in XML");
            }
            if (string.IsNullOrEmpty(fordelingsobjektAfsendRequest.anmodning.DistributionObject.ObjektIndhold.DistributionJournalPost.SagForslag))
            {
                throw new ArgumentException("Missing Sagforslag (Sag-id) in request");
            }

            return fordelingsobjektAfsendRequest;
        }

        private void ValidateAgainstXSD(string content)
        {
            try
            {               
                XmlReaderSettings xsdSettings = new XmlReaderSettings();
                string assemblyPath = Assembly.GetExecutingAssembly().CodeBase;
                assemblyPath = assemblyPath.Substring(0, assemblyPath.LastIndexOf("/"));

                xsdSettings.Schemas.Add(_targetNamespace, assemblyPath + _xsdPath);
                xsdSettings.ValidationType = ValidationType.Schema;
                xsdSettings.ValidationEventHandler += new ValidationEventHandler(xsdValidationEventHandler);

                XmlReader books = XmlReader.Create(new StringReader(content), xsdSettings); 

                while (books.Read()) { }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                LogService.LogToEventLog($"XSD Validation Error: " + ex.Message, System.Diagnostics.EventLogEntryType.Error);                
                throw;
            }
        }

        static void xsdValidationEventHandler(object sender, ValidationEventArgs e)
        {
            if (e.Severity == XmlSeverityType.Warning)
            {
                Console.Write("WARNING: ");
                Console.WriteLine(e.Message);
                LogService.LogToEventLog($"XSD Validation Warning: " + e.Message, System.Diagnostics.EventLogEntryType.Warning);
            }
            else if (e.Severity == XmlSeverityType.Error)
            {
                Console.Write("ERROR: ");
                Console.WriteLine(e.Message);
                LogService.LogToEventLog($"XSD Validation Error: " + e.Message, System.Diagnostics.EventLogEntryType.Warning);
                throw new XmlSchemaException(e.Message);
            }
        }

        private FordelingsobjektModtagResponse CreateFordelingsobjektModtagResponseReceived()
        {
            return new FordelingsobjektModtagResponse()
            {
                ForretningsKvittering = new ForretningsKvittering()
                {
                    ForretningsValideringsKode = "MODTAGET",
                    Kvitteringstype = "Teknisk"
                }
            };
        }

        private FordelingsobjektModtagResponse CreateFordelingsobjektModtagResponseRejected(Exception ex)
        {
            return new FordelingsobjektModtagResponse()
            {
                ForretningsKvittering = new ForretningsKvittering()
                {
                    ForretningsValideringsKode = "AFVIST",
                    Kvitteringstype = "Teknisk",
                    Begrundelse = ex.Message
                }
            };
        }
        #endregion        
    }
}