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
using System.Collections;
using System.Data;
using ExcelDataReader;
using ClosedXML.Excel;

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
        Task<TrackedreferenceResponse> GetTrackedreferenceAsync(string token, string reference);


    }
    public class DhisQueryService : IDhisQueryService
    {
        public List<ArrayList> RowFile = new List<ArrayList>();
        public List<ArrayList> RowFileLab = new List<ArrayList>();
        public string[] headers;
        public string[] headersLab;
        public StreamReader reader;
        public StreamReader readerLab;     
        public string error = "";
        public string startDate;
        public string endDate;
        public int contFiles = 0;
        public string nameFile;
        public string nameFileLab = "";
        public BinaryReader fileByteOrigin;
        public byte[] dataOrigin;
        public BinaryReader fileByteLabOrigin;
        public byte[] dataLabOrigin = null;
        public string ounitvalueFirst;
        public OrganisationUnit ouFirts = new OrganisationUnit();

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

            //byte[] data = null;
            //var fileByte = new BinaryReader(request.CsvFile.OpenReadStream());
            //int i = (int)request.CsvFile.Length;
            //data = fileByte.ReadBytes(i);

            List<validateDto> lv = new List<validateDto>();
            List<sumaryerrorDto> le = new List<sumaryerrorDto>();
            sumaryerrorDto sumerrorobj = new sumaryerrorDto();

            int iderror = 1;
            nameFile = request.CsvFile.FileName;

            string fileExtension = Path.GetExtension(request.CsvFile.FileName);
            if (fileExtension == ".csv")
            {
                RowFile = ReadCSV(request);
            }
            if (fileExtension == ".xlsx")
            {
                RowFile = ReadXLSX(request);
            }
            if (fileExtension == ".xls")
            {
                RowFile = ReadXLS(request);
            }

            if (fileExtension != ".csv")
            {
                if (fileExtension != ".xlsx")
                {
                    if (fileExtension != ".xls")
                    {
                        error += "\nEl tipo de archivo " + Path.GetExtension(request.CsvFile.FileName) + " " + request.CsvFile.FileName + "  no es compatible con los archivos aceptados (*.csv (separado por , ó ;), *.xls y *.xlsx)";
                        response = error;
                    }
                    }   
              

                {
                    var v = new validateDto
                    {
                        indexpreload = iderror++,
                        id = "",
                        detail = error,
                        ln = 0,
                        cl = 0,
                        ms = "",
                        errortype = "Extension file",
                        value = ""
                    };
                    lv.Add(v);
                    sumerrorobj.extensionfile = sumerrorobj.extensionfile + 1;
                }
            }

            //var reader = new StreamReader(request.CsvFile.OpenReadStream());
            var lstDate = new List<Int32>();
            //var propiedades = reader.ReadLine().Split(';');
            //propiedades = propiedades.Select(s => s.ToUpperInvariant().Trim()).ToArray();
            //startDate = request.startdate + "-01-01";
            //endDate = (request.enddate + 1) + "-01-01";
            
            try
            {
                var ExternalImportDataApp = await GetAllProgramAsync(request.token);
                objprogram = ExternalImportDataApp.Programs.Where(a => a.Programid.Equals(request.Programsid)).FirstOrDefault();        

                int colounits = Array.IndexOf(headers, objprogram.Orgunitcolumm.ToUpperInvariant());
                OrganisationUnitsDto Organisation = new OrganisationUnitsDto();
                Organisation = await GetAllOrganisation(request.token);
                var contentOrg = JsonConvert.SerializeObject(Organisation);

                string line;
                int ln = 1;              

               
                int cic = 0;
                while (cic < RowFile.Count)                   
                {
                    var valores = RowFile[cic];                    
                    cic++;
                    ln++;
                    //var valores = line.Split(';');                    
                    int dtRashOn = Array.IndexOf(headers, "DTRASHONSET"); //error
                    string dtRashOnval = valores[dtRashOn].ToString();
                    int caseid = Array.IndexOf(headers, "CASE_ID");
                    string caseidvalue = valores[caseid].ToString();
                    int flnid = Array.IndexOf(headers, "FIRST LAST NAME");
                    string flnvalue = valores[flnid].ToString();
                    int snid = Array.IndexOf(headers, "SECOND LAST NAME");
                    string snvalue = valores[snid].ToString();
                    int fnid = Array.IndexOf(headers, "FIRST NAME");
                    string fnvalue = valores[fnid].ToString();

                    

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
                            value = valores[dtRashOn].ToString()
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

                        //unidades organizativas              
                        OrganisationUnit ounitsFirst = new OrganisationUnit();
                        int colounitsFirst = Array.IndexOf(headers, objprogram.Orgunitcolumm.ToUpperInvariant());
                        if (colounitsFirst == -1)
                        {
                            if (String.IsNullOrEmpty(error))
                                error = "El archivo no tiene la estructura correcta, posiblemente no tiene la Unidad Organizativa";
                        }
                        var Firtsline = RowFile[0];
                        ounitvalueFirst = Firtsline[colounitsFirst].ToString();

                        ouFirts = Organisation.OrganisationUnits.Find(x => x.code == Firtsline[colounitsFirst].ToString());
                        oupath = ouFirts.path.Split("/")[2];
                        var setClean = await SetCleanEvent(oupath, objprogram.Programid, startDate, endDate, request.token);
                        objdryrunDto.Deleted = Convert.ToInt32(setClean.events.Count);
                        oupath = "OK";
                        //string ounitvalue = valores[colounits].ToString().ToUpper().Trim();
                        //foreach (OrganisationUnit ou in Organisation.OrganisationUnits)
                        //{
                        //    if (ou.code == ounitvalue)
                        //    {
                        //        oupath = ou.path.Split("/")[2];
                        //        var setClean = await SetCleanEvent(oupath, objprogram.Programid,startDate, endDate, request.token);
                        //        objdryrunDto.Deleted = Convert.ToInt32(setClean.events.Count);
                        //        oupath = "OK";

                        //    }
                        //    if (oupath != null)
                        //        break;
                        //}

                    }
                    int ideventdate = Array.IndexOf(headers, objprogram.Incidentdatecolumm.ToUpperInvariant());
                    string eventdate = valores[ideventdate].ToString(); //validar que no este null
                    List<Attribut> listAttribut = new List<Attribut>();

                    foreach (Queries.DTOs.Attribute at in objprogram.Attribute)//Validamos atributos
                    {
                        if (at.Column != null)
                        {
                            try
                            {
                                Attribut attribut = new Attribut();
                                var idval = Array.IndexOf(headers, at.Column.ToUpperInvariant());
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
                                            value = valores[idval].ToString()
                                        };
                                        lv.Add(v);
                                        sumerrorobj.mandatory = sumerrorobj.mandatory + 1;
                                    }
                                    if (at.mandatory == "true" && at.valueType.ToUpper().Trim() == "DATE")
                                    {

                                        DateTime atdate;
                                        if (!DateTime.TryParseExact(valores[idval].ToString(), "yyyy-mm-dd", null, System.Globalization.DateTimeStyles.None, out atdate) && at.Column.Trim().ToUpper() != "DTRASHONSET")
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
                                                value = valores[idval].ToString()
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
                                    int idval = Array.IndexOf(headers, dte.dataElement.column.ToUpperInvariant());
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
                                                value = valores[idval].ToString()
                                            };
                                            lv.Add(v);
                                            sumerrorobj.compulsory = sumerrorobj.compulsory + 1;
                                        }

                                        if (dte.compulsory.ToUpper().Trim() == "TRUE" && dte.dataElement.valueType.ToUpper().Trim() == "TEXT" && valores[idval].ToString().Trim().Length == 0)
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
                                                value = valores[idval].ToString()
                                            };
                                            lv.Add(v);
                                            sumerrorobj.compulsory = sumerrorobj.compulsory + 1;
                                        }
                                        if (dte.compulsory.ToUpper().Trim() == "TRUE" && dte.dataElement.optionSet.options.Count > 0 && valores[idval].ToString().Trim().Length > 0)
                                        {
                                            Boolean isok = false;
                                            foreach (Option opt in dte.dataElement.optionSet.options) {
                                                if (valores[idval].ToString() == opt.code)
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
                                                    value = valores[idval].ToString()
                                                };
                                                lv.Add(v);
                                                sumerrorobj.option = sumerrorobj.option + 1;
                                            }                                       
                                        }
                                        if (dte.compulsory == "true" && dte.dataElement.valueType.ToUpper().Trim() == "DATE")
                                        {

                                            DateTime atdate;
                                            if (!DateTime.TryParseExact(valores[idval].ToString(), "yyyy-mm-dd", null, System.Globalization.DateTimeStyles.None, out atdate))
                                            {
                                                var v = new validateDto
                                                {
                                                    id = caseidvalue,
                                                    detail = flnvalue.Trim() + " " + snvalue.Trim() + " " + fnvalue.Trim(),
                                                    ln = ln,
                                                    cl = idval+1,
                                                    ms = dte.dataElement.name.ToUpper(),
                                                    errortype = "dtformat",
                                                    value = valores[idval].ToString()
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
                sumerrorobj.totalrows = RowFile.Count;
                sumerrorobj.deletedEvents =objdryrunDto.Deleted;
                le.Add(sumerrorobj);
                
                status = "200";
                response = "Procesado correctamente";
            }
            catch (Exception e)
            {

                status =!string.IsNullOrEmpty(error)?error:e.Message;

            }
            objdryrunDto.Uploads = contupload;
            objdryrunDto.Response = lv; 
            objdryrunDto.State = status;
            objdryrunDto.Sumary = le;
            objdryrunDto.TotalFile1 = RowFile.Count;
            objdryrunDto.TotalFile2 = RowFileLab.Count;

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
            Console.Write("\nInicio AddTracked ");
            var content = JsonConvert.SerializeObject(request).Replace("Eenr", "enrollments").Replace("Eev", "events");
            var result = await RequestHttp.CallMethodSave("dhis", "addTracked", content, token);
            Console.Write("\nFin AddTracked " + result.ToString());
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
            Console.Write("\nInicio GetStateTask ");
            var result = await RequestHttp.CallMethodTask("dhis", "program", task, token);
            string resp = result.Replace("[", "{resultTasks: [").Replace("]", "]}");
            var j = JsonConvert.DeserializeObject<ResultTaskDto>(resp);
            Console.Write("\nFin GetStateTask : " + j.resultTasks[0].message.ToString() + " estado: " + j.resultTasks[0].completed.ToString());
            return j;
        }

        public async Task<string> GetSummaryImport(string category,string uid, string token)
        {
            Console.Write("\nInicio GetSummaryImport ");
            var result = await RequestHttp.CallMethodSummary("dhis", "program", uid, category, token);
            //string resp = result.Replace("[", "{resultTasks: [").Replace("]", "]}");
            //var x = JsonConvert.DeserializeObject<string>(result);
            Console.Write("\nFin GetSummaryImport: ", result.ToString());
            return result;
        }


        public List<ArrayList> ReadCSV(HistoryCreateCommandDto command)
        {
            try
            {       
                startDate = command.startdate + "-01-01";
                endDate = (command.enddate + 1) + "-01-01";

                if (command.CsvFile01 != null)
                {
                    string fileExtension = Path.GetExtension(command.CsvFile01.FileName);
                    if (fileExtension == ".csv")
                    {
                        readerLab = new StreamReader(command.CsvFile01.OpenReadStream());
                        headersLab = readerLab.ReadLine().Split(command.separator.ToString());
                        if (headersLab.Length <= 1)
                        {
                            error = "El segundo archivo no tiene el formato solicitado, separación por (,) ó (;)";
                            Console.Write("\nError de ReadCSV" + error.ToString());
                        }
                        headersLab = headersLab.Select(s => s.ToUpperInvariant()).ToArray();
                        string lineLab;
                        ArrayList listLab;
                        //List<ArrayList> RowFile = new List<ArrayList>();

                        while ((lineLab = readerLab.ReadLine()) != null)
                        {
                            contFiles = contFiles + 1;
                            var valores = lineLab.Split(command.separator.ToString());
                            string[] LineFile = valores.Select(s => s.ToUpperInvariant()).ToArray();
                            listLab = new ArrayList(LineFile);
                            RowFileLab.Add(listLab);
                        }
                        nameFileLab = command.CsvFile01.FileName;
                        fileByteLabOrigin = new BinaryReader(command.CsvFile01.OpenReadStream());
                        int f = (int)command.CsvFile01.Length;
                        dataLabOrigin = fileByteLabOrigin.ReadBytes(f);
                        readerLab.Close();
                    }
                    else
                    {
                        error = "El segundo archivo " + Path.GetExtension(command.CsvFile01.FileName) + " " + command.CsvFile01.FileName + "  no es compatible con los archivos aceptados (*.csv (separado por , ó ;), *.xls y *.xlsx)";
                     }
                }

                string line;
                ArrayList list;
                //List<ArrayList> RowFile = new List<ArrayList>();
                reader = new StreamReader(command.CsvFile.OpenReadStream());

                headers = reader.ReadLine().Split(command.separator.ToString());
                headers = headers.Select(s => s.ToUpperInvariant()).ToArray();
                if (headers.Length <= 1)
                {
                    error = "El arhivo no tiene el formato solicitado, separacion por (,) ó (;)";
                }
                while ((line = reader.ReadLine()) != null)
                {
                    contFiles = contFiles + 1;
                    var valores = line.Split(command.separator.ToString());
                    string[] LineFile = valores.Select(s => s.ToUpperInvariant()).ToArray();
                    list = new ArrayList(LineFile);
                    RowFile.Add(list);
                }

                fileByteOrigin = new BinaryReader(command.CsvFile.OpenReadStream());
                int i = (int)command.CsvFile.Length;
                dataOrigin = fileByteOrigin.ReadBytes(i);
                if (dataLabOrigin == null)
                {
                    dataLabOrigin = dataOrigin;
                }
                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                error = e.Message;
            }
            return RowFile;
        }

        public List<ArrayList> ReadXLSX(HistoryCreateCommandDto command)
        {
            try
            {                
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                Encoding srcEncoding = Encoding.GetEncoding(1251);

                var readerExcel = command.CsvFile.OpenReadStream();
                var workbook = new XLWorkbook(readerExcel);
                int countWS = workbook.Worksheets.Count;
                if (countWS == 2)
                {

                    var wsLab = workbook.Worksheet(2);
                    var RowsLab = wsLab.Rows().ToList();
                    var ColumnsLab = wsLab.Columns().ToList();

                    string[] LineFileLab = new string[ColumnsLab.Count()];
                    string[] headersExcelLab = new string[ColumnsLab.Count()];
                    ArrayList listLab;

                    List<DataValue> listDataValue = new List<DataValue>();

                    for (int i = 1; i <= RowsLab.Count(); i++)
                    {
                        var RowComplete = RowsLab[i - 1].Cells(1, ColumnsLab.Count()).ToList();
                        for (int j = 0; j < ColumnsLab.Count(); j++)
                        {
                            if (i == 1)
                            {
                                headersExcelLab[j] = RowComplete[j].Value.ToString().ToUpperInvariant();
                            }
                            else
                            {
                                var cellInd = RowComplete[j].Value.ToString();
                                var isDate = cellInd.Contains("12:00:00");

                                if (isDate)
                                {
                                    //string rec = cellInd.Substring(0, 10);
                                    string date = Convert.ToDateTime(cellInd).ToString("yyyy-MM-dd");
                                    LineFileLab[j] = date;
                                }
                                else
                                {
                                    LineFileLab[j] = RowComplete[j].Value.ToString();
                                }
                            }
                        }
                        headersLab = headersExcelLab;
                        if (LineFileLab[0] != null)
                        {
                            listLab = new ArrayList(LineFileLab);
                            RowFileLab.Add(listLab);
                        }
                    }
                }


                var ws = workbook.Worksheet(1);
                var Rows = ws.Rows().ToList();
                var Columns = ws.Columns().ToList();

                string[] LineFile = new string[Columns.Count()];
                string[] headersExcel = new string[Columns.Count()];
                ArrayList list;

                for (int i = 1; i <= Rows.Count(); i++)
                {
                    var RowComplete = Rows[i - 1].Cells(1, Columns.Count()).ToList();
                    for (int j = 0; j < Columns.Count(); j++)
                    {
                        if (i == 1)
                        {
                            headersExcel[j] = RowComplete[j].Value.ToString().ToUpperInvariant();
                        }
                        else
                        {
                            var cellInd = RowComplete[j].Value.ToString();
                            var isDate = cellInd.Contains("12:00:00");

                            if (isDate)
                            {
                                //string rec = cellInd.Substring(0, 10);
                                string date = Convert.ToDateTime(cellInd).ToString("yyyy-MM-dd");
                                LineFile[j] = date;
                            }
                            else
                            {
                                LineFile[j] = RowComplete[j].Value.ToString();
                            }

                        }
                    }
                    headers = headersExcel;
                    if (LineFile[0] != null)
                    {
                        list = new ArrayList(LineFile);
                        RowFile.Add(list);
                    }

                }


                fileByteOrigin = new BinaryReader(command.CsvFile.OpenReadStream());
                int k = (int)command.CsvFile.Length;
                dataOrigin = fileByteOrigin.ReadBytes(k);
                dataLabOrigin = dataOrigin;
                readerExcel.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                error = e.Message;
            }
            return RowFile;

        }

        public List<ArrayList> ReadXLS(HistoryCreateCommandDto command)
        {
            try
            {                
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                Encoding srcEncoding = Encoding.GetEncoding(1251);

                var readerExcel = command.CsvFile.OpenReadStream();
                var excelReader = ExcelReaderFactory.CreateBinaryReader(readerExcel);
                var dsexcelRecords = excelReader.AsDataSet();

                int CountPages = dsexcelRecords.Tables.Count;// important

                if (dsexcelRecords != null && dsexcelRecords.Tables.Count > 0)
                {
                    if (CountPages == 2)
                    {
                        DataTable dtDataRecords = dsexcelRecords.Tables[1];
                        var colums = dtDataRecords.Columns.Count;

                        string[] LineFile = new string[dtDataRecords.Columns.Count];
                        string[] headersExcel = new string[dtDataRecords.Columns.Count];
                        ArrayList list;

                        for (int i = 0; i < dtDataRecords.Rows.Count; i++)
                        {
                            for (int j = 0; j < dtDataRecords.Columns.Count; j++)
                            {
                                if (i == 0)
                                {
                                    headersExcel[j] = Convert.ToString(dtDataRecords.Rows[i][j].ToString().ToUpperInvariant());
                                }
                                else
                                {
                                    var cellInd = Convert.ToString(dtDataRecords.Rows[i][j].ToString());
                                    var isDate = cellInd.Contains("12:00:00");

                                    if (isDate)
                                    {
                                        //string rec = cellInd.Substring(0, 10);
                                        string date = Convert.ToDateTime(cellInd).ToString("yyyy-MM-dd");
                                        LineFile[j] = date;
                                    }
                                    else
                                    {
                                        LineFile[j] = cellInd;
                                    }
                                }
                            }
                            headersLab = headersExcel;
                            if (LineFile[0] != null)
                            {
                                list = new ArrayList(LineFile);
                                RowFileLab.Add(list);
                            }

                        }
                    }
                }

                if (dsexcelRecords != null && dsexcelRecords.Tables.Count > 0)
                {
                    DataTable dtDataRecords = dsexcelRecords.Tables[0];
                    var colums = dtDataRecords.Columns.Count;


                    string[] LineFile = new string[dtDataRecords.Columns.Count];
                    string[] headersExcel = new string[dtDataRecords.Columns.Count];
                    ArrayList list;

                    for (int i = 0; i < dtDataRecords.Rows.Count; i++)
                    {
                        for (int j = 0; j < dtDataRecords.Columns.Count; j++)
                        {
                            if (i == 0)
                            {
                                headersExcel[j] = Convert.ToString(dtDataRecords.Rows[i][j].ToString().ToUpperInvariant());
                            }
                            else
                            {
                                var cellInd = Convert.ToString(dtDataRecords.Rows[i][j].ToString());
                                var isDate = cellInd.Contains("12:00:00");

                                if (isDate)
                                {
                                    //string rec = cellInd.Substring(0, 10);
                                    string date = Convert.ToDateTime(cellInd).ToString("yyyy-MM-dd");
                                    LineFile[j] = date;
                                }
                                else
                                {
                                    LineFile[j] = cellInd;
                                }
                            }
                        }
                        headers = headersExcel;
                        if (LineFile[0] != null)
                        {
                            list = new ArrayList(LineFile);
                            RowFile.Add(list);
                        }

                    }
                }

                fileByteOrigin = new BinaryReader(command.CsvFile.OpenReadStream());
                int k = (int)command.CsvFile.Length;
                dataOrigin = fileByteOrigin.ReadBytes(k);
                dataLabOrigin = dataOrigin;
                readerExcel.Close();
                return RowFile;
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            return RowFile;
        }

        public async Task<TrackedreferenceResponse> GetTrackedreferenceAsync(string token, string reference)
        {
            var result = await RequestHttp.CallMethod("dhis", "trackedreference", token, reference);
            return JsonConvert.DeserializeObject<TrackedreferenceResponse>(result);
        }


    }
}
