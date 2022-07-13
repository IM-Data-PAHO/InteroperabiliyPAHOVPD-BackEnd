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
            List<validateDto> lv = new List<validateDto>();
            try
            {
                var ExternalImportDataApp = await GetAllProgramAsync(request.token);
                objprogram = ExternalImportDataApp.Programs.Where(a => a.Programid.Equals(request.Programsid)).FirstOrDefault();
                int colounits = Array.IndexOf(propiedades, objprogram.Orgunitcolumm.ToUpperInvariant());
                OrganisationUnitsDto Organisation = new OrganisationUnitsDto();
                Organisation = await GetAllOrganisation(request.token);
                var contentOrg = JsonConvert.SerializeObject(Organisation);

                string line;
                int ln = 1;

                while ((line = reader.ReadLine()) != null)
                {
                    ln++;
                    var valores = line.Split(';');
                    int dtRashOn = Array.IndexOf(propiedades, "DTRASHONSET"); //error
                    string dtRashOnval = valores[dtRashOn];
                    int caseid = Array.IndexOf(propiedades, "CASE_ID");
                    string caseidvalue = valores[caseid];
                    DateTime fecha;
                    if(!DateTime.TryParseExact(dtRashOnval, "yyyy-mm-dd", null, System.Globalization.DateTimeStyles.None, out fecha))
                    {
                        var v = new validateDto
                        {
                            detail = caseidvalue,
                            ln = ln,
                            cl = dtRashOn,
                            ms = "DTRASHONSET INVALID",
                            value = valores[dtRashOn]
                        };
                        lv.Add(v);
                    }
                    //else { 

                    //int dty = 0;
                    //try
                    //{
                    //    dty = Convert.ToInt32(Convert.ToDateTime(dtRashOnval).Year);
                    //}
                    //catch (Exception e) { Console.WriteLine("{0} Exception caught.", e); }
                    //if (dty == request.startdate || dty == request.enddate)
                    //{
                    //    contupload++;
                    //}
                    //}
                    if (oupath == null)
                    {

                        string ounitvalue = valores[colounits].ToUpper().Trim();
                        foreach (OrganisationUnit ou in Organisation.OrganisationUnits)
                        {
                            if (ou.code == ounitvalue)
                            {
                                oupath = ou.path.Split("/")[2];
                                var setClean = await SetCleanEvent(oupath, startDate, endDate, request.token);
                                objdryrunDto.Deleted = Convert.ToInt32(setClean.events.Count);
                                oupath = "OK";

                            }
                            if (oupath != null)
                                break;
                        }

                    }
                    int ideventdate = Array.IndexOf(propiedades, objprogram.Incidentdatecolumm.ToUpperInvariant());
                    string eventdate = valores[ideventdate]; //validar que no este null
                    List<Attribut> listAttribut = new List<Attribut>();

                    foreach (Queries.DTOs.Attribute at in objprogram.Attribute)//Validamos atributos
                    {
                        if (at.Column != null)
                        {
                            try
                            {
                                Attribut attribut = new Attribut();
                                var idval = Array.IndexOf(propiedades, at.Column.ToUpperInvariant());
                                attribut.attribute = at.Id;
                                if (idval >= 0)
                                {
                                    if (at.mandatory.ToUpper().Trim() == "TRUE" && valores[idval] == null)
                                    {
                                        var v = new validateDto
                                        {
                                            detail = caseidvalue,
                                            ln = ln,
                                            cl = idval+1,
                                            ms = at.Column.ToUpper() + " {{INVALID}}",
                                            value = valores[idval]
                                        };
                                        lv.Add(v);
                                   }
                                    if (at.mandatory == "true" && at.valueType.ToUpper().Trim() == "DATE")
                                    {

                                        DateTime atdate;
                                        if (!DateTime.TryParseExact(valores[idval], "yyyy-mm-dd", null, System.Globalization.DateTimeStyles.None, out atdate) && at.Column.Trim().ToUpper() != "DTRASHONSET")
                                        {
                                            var v = new validateDto
                                            {
                                                detail = caseidvalue,
                                                ln = ln,
                                                cl = idval+1,
                                                ms = at.Column.ToUpper() + " {{INVALID}}",
                                                value = valores[idval]
                                            };
                                            lv.Add(v);
                                        }
                                    }
                                }
                            }
                            catch (Exception e) { }
                        }
                    }
                    foreach (ProgramStage ps in objprogram.programStages) // validamos los dataelement
                    {
                        List<DataValue> listDataValue = new List<DataValue>();

                        try
                        {
                            foreach (ProgramStageDataElement dte in ps.programStageDataElements)
                            {

                                try
                                {
                                    DataValue datavalue = new DataValue();
                                    int idval = Array.IndexOf(propiedades, dte.dataElement.column.ToUpperInvariant());
                                    if (idval >= 0)
                                    {
                                        if (dte.compulsory.ToUpper().Trim() == "TRUE" && valores[idval] == null)
                                        {
                                            var v = new validateDto
                                            {
                                                detail = caseidvalue,
                                                ln = ln,
                                                cl = idval+1,
                                                ms = dte.dataElement.name.ToUpper() + " {{INVALID}}",
                                                value = valores[idval]
                                            };
                                            lv.Add(v);
                                        }

                                        if (dte.compulsory.ToUpper().Trim() == "TRUE" && dte.dataElement.valueType.ToUpper().Trim() == "TEXT" && valores[idval].Trim().Length == 0)
                                        {
                                            var v = new validateDto
                                            {
                                                detail = caseidvalue,
                                                ln = ln,
                                                cl = idval+1,
                                                ms = dte.dataElement.name.ToUpper() + " {{INVALID}}",
                                                value = valores[idval]
                                            };
                                            lv.Add(v);
                                        }
                                        if (dte.compulsory.ToUpper().Trim() == "TRUE" && dte.dataElement.optionSet.options.Count > 0 && valores[idval].Trim().Length > 0)
                                        {
                                            Boolean isok = false;

                                            foreach (Option opt in dte.dataElement.optionSet.options) {
                                                if (valores[idval] == opt.code)
                                                {
                                                    isok = true;
                                                    break;
                                                }
                                            }
                                            if (isok == false)
                                            {
                                                var v = new validateDto
                                                {
                                                    detail = caseidvalue,
                                                    ln = ln,
                                                    cl = idval+1,
                                                    ms = dte.dataElement.name.ToUpper() + " {{OPTION INVALID}}",
                                                    value = valores[idval]
                                                };
                                                lv.Add(v);
                                            }                                       
                                        }
                                        if (dte.compulsory == "true" && dte.dataElement.valueType.ToUpper().Trim() == "DATE")
                                        {

                                            DateTime atdate;
                                            if (!DateTime.TryParseExact(valores[idval], "yyyy-mm-dd", null, System.Globalization.DateTimeStyles.None, out atdate))
                                            {
                                                var v = new validateDto
                                                {
                                                    detail = caseidvalue,
                                                    ln = ln,
                                                    cl = idval+1,
                                                    ms = dte.dataElement.name.ToUpper() + " {{INVALID}}",
                                                    value = valores[idval]
                                                };
                                                lv.Add(v);
                                            }
                                        }
                                    }
                                }
                                catch (Exception e) { }//contbad = contbad + 1; }

                            }

                        }
                        catch (Exception e) { }
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
            objdryrunDto.Response = lv; 
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
            var content = JsonConvert.SerializeObject(request).Replace("Eenr", "enrollments").Replace("Eev", "events");
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
