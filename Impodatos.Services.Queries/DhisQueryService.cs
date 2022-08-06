﻿using Impodatos.Services.Common.Security;
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
        Task<OrganisationUnit> GetOrganisationUnit(string token, string uid);
        Task<UidGeneratedDto> GetUidGenerated(string quantity, string token);
        Task<AddTrackedDto> GetTracket(string caseid, string ou, string token);
        Task<AddEnrollmentsClearDto> GetEnrollment(string program, string oupath,  string token);
        Task<AddTracketResultDto> AddTracked(AddTrackedDto request, string token);
        Task<AddEnrollmentResultDto> AddEnrollment(AddEnrollmentDto request, string token);
        Task<ResponseDhis> AddEventClear(AddEventsClearDto request, string token, string strategy = "");
        Task<ResponseDhis> AddEnrollmentClear(AddEnrollmentsClearDto request, string token, string strategy = "");
        Task<AddEventResultDto> AddEvent(AddEventsDto request, string token);
        Task<List<SequentialDto>> GetSequential(string quantity, string token);
        Task<AddEventsClearDto> SetCleanEvent(string oupath, string program ,string startDate, string endDate, string token);
        Task<AddEnrollmentsClearDto> SetCleanEnrollment(string oupath, string tracked, string startDate, string endDate, string token);
        Task<ResultTaskDto> GetStateTask(string task, string token);
        Task<string> GetSummaryImport(string category,string uid, string token);


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
            propiedades = propiedades.Select(s => s.ToUpperInvariant().Trim()).ToArray();
            string startDate = request.startdate + "-01-01";
            string endDate = (request.enddate + 1) + "-01-01";
            List<validateDto> lv = new List<validateDto>();
            List<sumaryerrorDto> le = new List<sumaryerrorDto>();
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
                int iderror = 1;

                sumaryerrorDto sumerrorobj = new sumaryerrorDto();
                while ((line = reader.ReadLine()) != null)
                {
                    ln++;
                    var valores = line.Split(';');
                    int dtRashOn = Array.IndexOf(propiedades, "DTRASHONSET"); //error
                    string dtRashOnval = valores[dtRashOn];
                    int caseid = Array.IndexOf(propiedades, "CASE_ID");
                    string caseidvalue = valores[caseid];
                    int flnid = Array.IndexOf(propiedades, "FIRST LAST NAME");
                    string flnvalue = valores[flnid];
                    int snid = Array.IndexOf(propiedades, "SECOND LAST NAME");
                    string snvalue = valores[snid];
                    int fnid = Array.IndexOf(propiedades, "FIRST NAME");
                    string fnvalue = valores[fnid];

                    

                    DateTime fecha;
                    if(!DateTime.TryParseExact(dtRashOnval, "yyyy-mm-dd", null, System.Globalization.DateTimeStyles.None, out fecha))
                    {
                        var v = new validateDto
                        {
                            indexpreload = iderror++,
                            id = caseidvalue,
                            detail = flnvalue.Trim()+" "+snvalue.Trim() + " "+ fnvalue.Trim(),
                            ln = ln,
                            cl = dtRashOn+1,
                            ms = "DTRASHONSET",
                            errortype ="dtformat",
                            value = valores[dtRashOn]
                        };
                        lv.Add(v);
                        sumerrorobj.date = sumerrorobj.date + 1;
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
                                var setClean = await SetCleanEvent(oupath, objprogram.Programid,startDate, endDate, request.token);
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
                                             indexpreload = iderror++,
                                            id = caseidvalue,
                                            detail = flnvalue.Trim() + " " + snvalue.Trim() + " " + fnvalue.Trim(),
                                            ln = ln,
                                            cl = idval+1,
                                            ms = at.Column.ToUpper(),
                                            errortype = "mandatory",
                                            value = valores[idval]
                                        };
                                        lv.Add(v);
                                        sumerrorobj.mandatory = sumerrorobj.mandatory + 1;
                                    }
                                    if (at.mandatory == "true" && at.valueType.ToUpper().Trim() == "DATE")
                                    {

                                        DateTime atdate;
                                        if (!DateTime.TryParseExact(valores[idval], "yyyy-mm-dd", null, System.Globalization.DateTimeStyles.None, out atdate) && at.Column.Trim().ToUpper() != "DTRASHONSET")
                                        {
                                            var v = new validateDto
                                            {
                                                 indexpreload = iderror++,
                                                id = caseidvalue,
                                                detail = flnvalue.Trim() + " " + snvalue.Trim() + " " + fnvalue.Trim(),
                                                ln = ln,
                                                cl = idval+1,
                                                ms = at.Column.ToUpper(),
                                                errortype = "dtformat",
                                                value = valores[idval]
                                            };
                                            lv.Add(v);
                                            sumerrorobj.date = sumerrorobj.date + 1;
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
                                                indexpreload = iderror++,
                                                id = caseidvalue,
                                                detail = flnvalue.Trim() + " " + snvalue.Trim() + " " + fnvalue.Trim(),
                                                ln = ln,
                                                cl = idval + 1,
                                                ms = dte.dataElement.name.ToUpper(),
                                                errortype = "compulsory",
                                                value = valores[idval]
                                            };
                                            lv.Add(v);
                                            sumerrorobj.compulsory = sumerrorobj.compulsory + 1;
                                        }

                                        if (dte.compulsory.ToUpper().Trim() == "TRUE" && dte.dataElement.valueType.ToUpper().Trim() == "TEXT" && valores[idval].Trim().Length == 0)
                                        {
                                            var v = new validateDto
                                            {
                                                 indexpreload = iderror++,
                                                id = caseidvalue,
                                                detail = flnvalue.Trim() + " " + snvalue.Trim() + " " + fnvalue.Trim(),
                                                ln = ln,
                                                cl = idval+1,
                                                ms = dte.dataElement.name.ToUpper(),
                                                errortype = "compulsory",
                                                value = valores[idval]
                                            };
                                            lv.Add(v);
                                            sumerrorobj.compulsory = sumerrorobj.compulsory + 1;
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
                                                    indexpreload = iderror++,
                                                    id = caseidvalue,
                                                    detail = flnvalue.Trim() + " " + snvalue.Trim() + " " + fnvalue.Trim(),
                                                    ln = ln,
                                                    cl = idval+1,
                                                    ms = dte.dataElement.name.ToUpper(),
                                                    errortype = "option",
                                                    value = valores[idval]
                                                };
                                                lv.Add(v);
                                                sumerrorobj.option = sumerrorobj.option + 1;
                                            }                                       
                                        }
                                        if (dte.compulsory == "true" && dte.dataElement.valueType.ToUpper().Trim() == "DATE")
                                        {

                                            DateTime atdate;
                                            if (!DateTime.TryParseExact(valores[idval], "yyyy-mm-dd", null, System.Globalization.DateTimeStyles.None, out atdate))
                                            {
                                                var v = new validateDto
                                                {
                                                    id = caseidvalue,
                                                    detail = flnvalue.Trim() + " " + snvalue.Trim() + " " + fnvalue.Trim(),
                                                    ln = ln,
                                                    cl = idval+1,
                                                    ms = dte.dataElement.name.ToUpper(),
                                                    errortype = "dtformat",
                                                    value = valores[idval]
                                                };
                                                lv.Add(v);
                                                sumerrorobj.date = sumerrorobj.date + 1;
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
                le.Add(sumerrorobj);
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
            objdryrunDto.Sumary = le;
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
            var result = await RequestHttp.CallMethodSave("dhis", "addTracked", content, token);
            return JsonConvert.DeserializeObject<AddTracketResultDto>(result);
        }
        public async Task<AddEnrollmentResultDto> AddEnrollment(AddEnrollmentDto request, string token)
        {
            var result="";
            try
            {
                var content = JsonConvert.SerializeObject(request);
                result = await RequestHttp.CallMethodSave("dhis", "enrollments", content, token);
            }
            catch (Exception e) { }
            return JsonConvert.DeserializeObject<AddEnrollmentResultDto>(result);
        }
        public async Task<ResponseDhis> AddEventClear(AddEventsClearDto request, string token, string strategy = "")
        {
            var content = JsonConvert.SerializeObject(request);
            content = content.Replace("event_", "event");
            var result = await RequestHttp.CallMethodSave("dhis", "events", content, token, strategy);
            return JsonConvert.DeserializeObject<ResponseDhis>(result);
        }
        public async Task<ResponseDhis> AddEnrollmentClear(AddEnrollmentsClearDto request, string token, string strategy = "")
        {
            var content = JsonConvert.SerializeObject(request);
            content = content.Replace("enrollment_", "enrollment");
            var result = await RequestHttp.CallMethodSave("dhis", "enrollments", content, token, strategy);
            return JsonConvert.DeserializeObject<ResponseDhis>(result);
        }
        public async Task<AddEventResultDto> AddEvent (AddEventsDto request, string token)
        {
            var content = JsonConvert.SerializeObject(request);
            content = content.Replace("event_","event");
            var result = await RequestHttp.CallMethodSave("dhis", "events", content, token);
            return JsonConvert.DeserializeObject<AddEventResultDto>(result);
        }
        public async Task<OrganisationUnitsDto> GetAllOrganisation(string token)
        {
            var result = await RequestHttp.CallMethod("dhis", "organisationUnits", token);
            return JsonConvert.DeserializeObject<OrganisationUnitsDto>(result);
        }
        public async Task<OrganisationUnit> GetOrganisationUnit(string token, string uid)
        {
            var result = await RequestHttp.CallMethodOUCountry("dhis", "organisationUnits/" + uid, token);
            return JsonConvert.DeserializeObject<OrganisationUnit>(result);
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
        public async Task<AddEnrollmentsClearDto> GetEnrollment(string program, string oupath,  string token)
        {
            var result = await RequestHttp.CallMethodClearEnrollments("dhis", "validateenroll", oupath, "", "" ,program,token);
            return JsonConvert.DeserializeObject<AddEnrollmentsClearDto>(result);
        }
        public async Task<AddEventsClearDto> SetCleanEvent(string oupath, string program, string startDate, string endDate, string token)
        {
            var result = await RequestHttp.CallMethodClear("dhis", "events", oupath,program, startDate, endDate, token);
            result = result.Replace("event", "event_");
            result = result.Replace("event_s", "events");
            return JsonConvert.DeserializeObject<AddEventsClearDto>(result);
        }
        public async Task<AddEnrollmentsClearDto> SetCleanEnrollment(string oupath, string program, string startDate, string endDate, string token)
        {
            var result = await RequestHttp.CallMethodClearEnrollments("dhis", "validateenroll", oupath, program,startDate, endDate, token);
            //result = result.Replace("enrollment", "enrollment_");
            //result = result.Replace("enrollment_s", "enrollments");
            return JsonConvert.DeserializeObject<AddEnrollmentsClearDto>(result);
        }
        public async Task<ResultTaskDto> GetStateTask(string task, string token)
        {
            var result = await RequestHttp.CallMethodTask("dhis", "program", task, token);
            string resp = result.Replace("[", "{resultTasks: [").Replace("]", "]}");
            var j = JsonConvert.DeserializeObject<ResultTaskDto>(resp);
            return j;
        }

        public async Task<string> GetSummaryImport(string category,string uid, string token)
        {
            var result = await RequestHttp.CallMethodSummary("dhis", "program", uid, category, token);
            //string resp = result.Replace("[", "{resultTasks: [").Replace("]", "]}");
            //var x = JsonConvert.DeserializeObject<string>(result);
            return result;
        }
    }
}
