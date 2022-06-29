using Impodatos.Services.Common.Security;
using Impodatos.Services.Queries.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using Newtonsoft.Json;
using static Impodatos.Services.Queries.DTOs.ResponseDhisDto;
using System.IO;

namespace Impodatos.Services.Queries
{
    public interface IDhisQueryService
    {
        void SetMaintenanceAsync(string token);
        Task<dryrunDto> StartDryRunAsync(HistoryCreateCommandDto request);
        //Task<DhisProgramDto> StartDryRunAsync(string token);
        Task<DhisProgramDto> GetAllProgramAsync(string token);
        Task<OrganisationUnitsDto> GetAllOrganisation(string token);
        Task<UidGeneratedDto> GetUidGenerated(string quantity, string token);
        Task<AddTrackedDto> GetTracket(string caseid, string ou, string token);
        Task<AddEnrollmentDto> GetEnrollment(string tracked, string ou, string token);
        Task<AddTracketResultDto> AddTracked(AddTrackedDto request, string token);
        Task<AddEnrollmentResultDto> AddEnrollment(AddEnrollmentDto request, string token);
        Task<ResponseDhis> AddEventClear(AddEventsClearDto request, string token, string strategy = "");
        Task<AddEventResultDto> AddEvent(AddEventsDto request, string token);
        Task<List<SequentialDto>> GetSequential(string quantity, string token);
        Task<AddEventsClearDto> SetCleanEvent(string oupath, string startDate, string endDate, string token);
    }
    public class DhisQueryService : IDhisQueryService
    {
        public async Task<dryrunDto> StartDryRunAsync(HistoryCreateCommandDto request)
        {
            dryrunDto objdryrunDto = new dryrunDto();
            string oupath = null;
            int contupload = 0;
            int contdelete = 0;
            string status = "";
            string response = "";
            List<string> jsonResponse = new List<string>();
            Program objprogram = new Program();

            byte[] data = null;
            var fileByte = new BinaryReader(request.CsvFile.OpenReadStream());
            int i = (int)request.CsvFile.Length;
            data = fileByte.ReadBytes(i);

            var reader = new StreamReader(request.CsvFile.OpenReadStream());
            var lstDate = new List<Int32>();
            var propiedades = reader.ReadLine().Split(';');
            propiedades = propiedades.Select(s => s.ToUpperInvariant()).ToArray();
            string startDate = request.startdate + "-01-01";
            string endDate = (request.enddate + 1) + "-01-01";

            try
            {
                var ExternalImportDataApp = await GetAllProgramAsync(request.token);
                objprogram = ExternalImportDataApp.Programs.Where(a => a.Programid.Equals(request.Programsid)).FirstOrDefault();
                int colounits = Array.IndexOf(propiedades, objprogram.Orgunitcolumm.ToUpperInvariant());
                OrganisationUnitsDto Organisation = new OrganisationUnitsDto();
                Organisation = await GetAllOrganisation(request.token);
                var contentOrg = JsonConvert.SerializeObject(Organisation);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    var valores = line.Split(';');
                    int dtRashOn = Array.IndexOf(propiedades, "DTRASHONSET"); //error
                    string dtRashOnval = valores[dtRashOn];
                    int dty = 0;
                    try
                    {
                        dty = Convert.ToInt32(Convert.ToDateTime(dtRashOnval).Year);
                    }
                    catch (Exception e) { Console.WriteLine("{0} Exception caught.", e); }
                    if (dty == request.startdate || dty == request.enddate)
                    {
                        contupload++;
                    }
                    if (oupath == null)
                    {
                        string ounitvalue = valores[colounits].ToUpper().Trim();
                        foreach (OrganisationUnit ou in Organisation.OrganisationUnits)
                        {
                            if (ou.code == ounitvalue)
                            {
                                oupath = ou.path.Split("/")[2];
                                var setClean = await SetCleanEvent(oupath, startDate, endDate, request.token);
                                contdelete = Convert.ToInt32(setClean.events.Count);
                                oupath = "OK";

                            }
                            if (oupath != null)
                                break;
                        }

                    }
                }
                status = "200";
                response = "Procesado correctamente";
            }
            catch (Exception e)
            {
                status = e.Message;

            }
            objdryrunDto.Uploads = contupload;
            objdryrunDto.Deleted = contdelete;
            objdryrunDto.Response = response;
            objdryrunDto.State = status;
            return objdryrunDto;
        }
        public async Task<DhisProgramDto> GetAllProgramAsync(string token )
        {
            var result = await RequestHttp.CallMethod("dhis", "program", token);           
             return JsonConvert.DeserializeObject<DhisProgramDto>(result);
        }
        public void SetMaintenanceAsync(string token)
        {
            var result =  RequestHttp.CallMethod("dhis", "maintenance", token);
        }
        public async Task<UidGeneratedDto> GetUidGenerated(string quantity, string token)
        {
            var result = await RequestHttp.CallGetMethod("dhis", "uidGenerated", quantity,"" ,token);
            return JsonConvert.DeserializeObject<UidGeneratedDto>(result);
        }
        public async Task<AddTracketResultDto> AddTracked(AddTrackedDto request, string token)
        {
            var content = JsonConvert.SerializeObject(request);
            var result = await RequestHttp.CallMethod("dhis", "addTracked", content, token);
            return JsonConvert.DeserializeObject<AddTracketResultDto>(result);
        }
        public async Task<AddEnrollmentResultDto> AddEnrollment(AddEnrollmentDto request, string token)
        {
            var result="";
            try
            {
                var content = JsonConvert.SerializeObject(request);
                result = await RequestHttp.CallMethod("dhis", "enrollments", content, token);
            }
            catch (Exception e) { }
            return JsonConvert.DeserializeObject<AddEnrollmentResultDto>(result);
        }
        public async Task<ResponseDhis> AddEventClear(AddEventsClearDto request, string token, string strategy = "")
        {
            var content = JsonConvert.SerializeObject(request);
            content = content.Replace("event_", "event");
            var result = await RequestHttp.CallMethod("dhis", "events", content, token, strategy);
            return JsonConvert.DeserializeObject<ResponseDhis>(result);
        }
        public async Task<AddEventResultDto> AddEvent (AddEventsDto request, string token)
        {
            var content = JsonConvert.SerializeObject(request);
            content = content.Replace("event_","event");
            var result = await RequestHttp.CallMethod("dhis", "events", content, token);
            return JsonConvert.DeserializeObject<AddEventResultDto>(result);
        }
        public async Task<OrganisationUnitsDto> GetAllOrganisation(string token)
        {
            var result = await RequestHttp.CallMethod("dhis", "organisationUnits", token);
            return JsonConvert.DeserializeObject<OrganisationUnitsDto>(result);
        }
        public async Task<List<SequentialDto>> GetSequential(string quantity, string token)
        {
            var result = await RequestHttp.CallGetMethod("dhis", "sequential", quantity, "",token);
            return JsonConvert.DeserializeObject<List<SequentialDto>>(result);
        }
        public async Task<AddTrackedDto> GetTracket(string caseid, string ou, string token)
        {
            var result = await RequestHttp.CallGetMethod("dhis", "validatetrak", caseid, ou, token);
            return JsonConvert.DeserializeObject<AddTrackedDto>(result);
        }
        public async Task<AddEnrollmentDto> GetEnrollment(string tracked, string ou, string token)
        {
            var result = await RequestHttp.CallGetMethod("dhis", "validateenroll", tracked, ou, token);
            return JsonConvert.DeserializeObject<AddEnrollmentDto>(result);
        }
        public async Task<AddEventsClearDto> SetCleanEvent(string oupath, string startDate, string endDate, string token)
        {
            var result = await RequestHttp.CallMethodClear("dhis", "events", oupath, startDate, endDate, token);
            result = result.Replace("event", "event_");
            result = result.Replace("event_s", "events");
            return JsonConvert.DeserializeObject<AddEventsClearDto>(result);
        }
    }
}
