using Impodatos.Domain;
using Impodatos.Persistence.Database;
using Impodatos.Services.EventHandlers.Commands;
using Impodatos.Services.Queries;
using Impodatos.Services.Queries.DTOs;
using MediatR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections;
using Npgsql;
using NpgsqlTypes;
using System.Data;
using System.Text;
using ClosedXML.Excel;
using ExcelDataReader;

namespace Impodatos.Services.EventHandlers
{
    public class ImportHistoryHandlerAsync : INotificationHandler<historyCreateCommand>,
         INotificationHandler<historyUpdateCommand>


    {
        private readonly ApplicationDbContext _context;
        private readonly IDhisQueryService _dhis;
        public LoginQueryService loginQueyService = new LoginQueryService();
        public int state = 1;
        public string oupath;
        public string startDate;
        public string endDate;
        public string program;
        public string token;
        public string dataIterable;
        public int cont = 0;
        public int contBlock = 0;
        public bool uploadBlock;
        public historyCreateCommand commandGeneral;
        public int contFiles = 0;
        public int blockSuccess = 0;
        public List<string> summaryImport = new List<string>();
        public bool endWhile = false;
        public UserSettingDto userSetting;
        public string nameFile;
        public string nameFileLab = "";
        public string ounitvalueFirst;
        List<ArrayList> RowFile = new List<ArrayList>();
        List<ArrayList> RowFileLab = new List<ArrayList>();
        public string[] headers;
        public string[] headersLab;
        public Program objprogram = new Program();
        public BinaryReader fileByteOrigin;
        public byte[] dataOrigin;
        public BinaryReader fileByteLabOrigin;
        public byte[] dataLabOrigin = null;
        public OrganisationUnit ouFirts = new OrganisationUnit();
        public List<string> jsonResponse = new List<string>();
        public StreamReader reader;
        public StreamReader readerLab;
        public BackgroundTask backgroundTask = new BackgroundTask();
        public SendMail sendMailObj = new SendMail();
        public string error;
        public string country;
        public string response = "";
        public OrganisationUnitsDto Organisation = new OrganisationUnitsDto();
        public string level;
        public bool completed= false;

        public ImportHistoryHandlerAsync(ApplicationDbContext context, IDhisQueryService dhis)
        {
            _context = context;
            _dhis = dhis;
        }

        private static ImportSettings _importSettings
        {
            get
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true)
                    .Build();

                return new ImportSettings
                {
                    Services = configuration.GetSection("ImportSettings:setting").GetChildren().Select(s => new ImportInt
                    {
                        Async = Convert.ToBoolean(s.GetSection("async").Value),
                        SizeUpload = Convert.ToInt32(s.GetSection("sizeUpload").Value),
                        Individual = Convert.ToBoolean(s.GetSection("individual").Value),
                        Block = Convert.ToBoolean(s.GetSection("block").Value),
                        Server = Convert.ToString(s.GetSection("server").Value),
                        EmailFrom = Convert.ToString(s.GetSection("emailFrom").Value),
                        Subject = Convert.ToString(s.GetSection("subject").Value),
                        Body = Convert.ToString(s.GetSection("body").Value),
                        Pass = Convert.ToString(s.GetSection("pass").Value),
                        Port = Convert.ToString(s.GetSection("port").Value)
                    }).ToList()
                };

            }
        }

        private static ConexionInt _conexionInt
        {
            get
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.Development.json", optional: true, reloadOnChange: true)
                    .Build();

                return new ConexionInt
                {
                    ConexionDatabase = configuration.GetSection("ConnectionStrings:ConexionDatabase").Value

                };
            }
        }

        public async Task OrchestratorAsync(string oupath, string startDate, string endDate, string program, string token, List<ArrayList> RowFile)
        {

            string server = _importSettings.Services[0].Server;

            var TaskResult = "";
            int step = 1;
            try
            {
                do
                {
                    switch (state)
                    {
                        case 1:
                            step = 1;
                            TaskResult = await CleanEventsAsync(oupath, program, startDate, endDate, token);
                            break;
                        case 2:
                            step = 2;
                            await CleanEnrollmentsAsync(program, oupath, startDate, endDate, token);
                            break;
                        case 3:
                            step = 3;
                            await ImportDataAsync(RowFile);
                            if (endWhile)
                            {
                                //if ((contBlock == blockSuccess) || (contBlock + 1 == blockSuccess))
                                //{
                                state = 4;
                                //}
                            }
                            break;
                        case 4:
                            SaveSummaryImport(commandGeneral.UserLogin, objprogram.Programname);
                            break;

                        case 5:
                            if (level != "ERROR")
                            {
                                sendMailObj.SenEmailImport(_importSettings.Services[0].Server, _importSettings.Services[0].Subject, _importSettings.Services[0].Body, userSetting.email, _importSettings.Services[0].EmailFrom, _importSettings.Services[0].Pass, _importSettings.Services[0].Port, "El archivo: " + nameFile);
                                state = 6;
                                break;
                            }
                            break;                            
                    }
                    if (TaskResult != "")
                    {
                        await CheckImportStateAsync(state, step, TaskResult, token, 1);
                    }
                }
                while (state < 6);

            }
            catch (Exception e)
            {
                error = e.Message;
                EmailErrorImport();
                commandGeneral.reponse = e.Message;
                Console.WriteLine("{0} Exception caught.", e);

            }
        }

        public async Task<bool> CheckImportStateAsync(int state, int step, string task, string token, int numImport = 1)
        {
            try
            {
                var response = await _dhis.GetStateTask(task.Replace("/api", ""), token);
                var completed = response.resultTasks[0].completed;
                var level = response.resultTasks[0].level;
                if (!completed)
                {
                    await CheckImportStateAsync(state, step, task, token, numImport);
                    return false;
                }
                else
                {
                    if (level == "ERROR") {
                        error = response.resultTasks[0].message;                       
                        Console.Write("\nError Eliminación de Eventos : " + error.ToString());
                        EmailErrorImport();
                    }
                    switch (step)
                    {
                        case 1:
                            state = 2;
                            Console.Write("\nFin Eliminación de Eventos");
                            break;
                        case 2:
                            state = 3;
                            break;
                        case 3:
                            state = 4;
                            break;
                    }
                    return true;
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                return false;
            }
        }

        public async Task<string> CleanEventsAsync(string oupath, string program, string startDate, string endDate, string token)
        {
            try
            {
                int contdelete = 0;
                Console.Write("\nInicio de Eliminación de Eventos ");
                var setClean = await _dhis.SetCleanEvent(oupath, program, startDate, endDate, token);
                if (setClean.events.Count > 0)
                {
                    var dropEvens = await _dhis.AddEventClear(setClean, token, "?strategy=DELETE&includeDeleted=true&async=true");
                    contdelete = Convert.ToInt32(dropEvens.response.total);
                    _dhis.SetMaintenanceAsync(token);
                    return dropEvens.response.relativeNotifierEndpoint.Replace("/api", "");
                }
                else
                {
                    state = 2;
                    Console.Write("\nSin Eliminación de Eventos ");
                }
                return "";
            }
            catch (Exception e)
            {
                error = e.Message;
                Console.Write("\nError de Eliminación de Eventos; ", error);
                EmailErrorImport();
                return "";
            }
        }

        public async Task CleanEnrollmentsAsync(string program, string oupath, string startDate, string endDate, string token)
        {
            try
            {
                int contdelete = 0;
                Console.Write("\nInicio de Eliminación de Enrollments");
                //var setCleanEnrolloment = await _dhis.GetEnrollment(program, oupath,  token);
                var setCleanEnrolloment = await _dhis.SetCleanEnrollment(oupath, program, startDate, endDate, token);
                if (setCleanEnrolloment.enrollments.Count > 0)
                {
                    var dropEnrrollments = await _dhis.AddEnrollmentClear(setCleanEnrolloment, token, "?strategy=DELETE");
                    contdelete = Convert.ToInt32(dropEnrrollments.response.total);
                    _dhis.SetMaintenanceAsync(token);
                    if (contdelete > 0)
                    {
                        state = 3;
                        Console.Write("\nFin de Eliminación de Enrollments");
                    }
                    else {
                        error = dropEnrrollments.message;
                        Console.Write("\nError de Eliminación de Enrollments" + error.ToString());
                        EmailErrorImport();
                    }
                }
                else
                {
                    state = 3;
                    Console.Write("\nSin Eliminación de Enrollments");
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                Console.Write("\nError Eliminación de Enrollments: ", error);
                EmailErrorImport();
            }
        }

        public async Task ImportDataAsync(List<ArrayList> RowFile)
        {
            try
            {
                var _set = _importSettings;
                int cont = 0;
                int contbad = 0;
                int SizeUpload = _set.Services[0].SizeUpload;               

                AddTrackedDto trackedDto = new AddTrackedDto();
                AddTrackedDto trackedDtos = new AddTrackedDto();

                List<TrackedEntityInstances> listtrackedInstDtoFull = new List<TrackedEntityInstances>();
                List<Enrollment> listEnrollmentFull = new List<Enrollment>();

                AddEnrollmentDto enrollments = new AddEnrollmentDto();
                AddEventsDto eventDto = new AddEventsDto();
                Console.Write("\nInicio importación de data, cantidad de registros: ", RowFile.Count);
                int cic = 0;
                while (cic < RowFile.Count)
                {
                    contFiles = contFiles + 1;
                    var valores = RowFile[cic];

                    Console.Write("Ciclos: " + cic.ToString());
                    cic++;
                    int dtRashOn = Array.IndexOf(headers, "DTRASHONSET"); //error
                    string dtRashOnval = valores[dtRashOn].ToString();
                    int dty = 0;
                    try
                    {
                        dty = Convert.ToInt32(Convert.ToDateTime(dtRashOnval).Year);
                    }
                    catch (Exception e) { Console.WriteLine("{0} Exception caught.", e); }
                    if (dty == commandGeneral.startdate || dty == commandGeneral.enddate)
                    {
                        List<TrackedEntityInstances> listtrackedInstDto = new List<TrackedEntityInstances>();
                        List<Enrollment> listEnrollment = new List<Enrollment>();

                        AddEnrollmentDto enrollment = new AddEnrollmentDto();
                        string trackedEntityInstance = "";
                        //if(codes == 9998) { TrackeduidGeneratedDto = await _dhis.GetUidGenerated("10000", command.token); codes = 0; }
                        //Genero un uid 
                        var TrackeduidGeneratedDto = await _dhis.GetUidGenerated("1", commandGeneral.token);
                        //leemos cada una de las lineas a partir de la linea dos con el metodo ReadLine, el cual va iterando cada linea

                        cont = cont + 1;
                        //cargar las unidades organizativas en un array para recorrerlo localmente y friltrar para crear el tracked
                        TrackedEntityInstances trackedInstDto = new TrackedEntityInstances();

                        int caseid = Array.IndexOf(headers, "CASE_ID");
                        string caseidvalue = valores[caseid].ToString();
                        Console.Write("\nCASE ID: " + caseidvalue.ToString());
                        //Validamos si el tracked ya existe:  verificar por que se estan creando repetidos
                        var validatetraked = await _dhis.GetTracket(caseidvalue, ouFirts.id, commandGeneral.token);
                        if (validatetraked.trackedEntityInstances.Count > 0)
                            trackedInstDto.trackedEntityInstance = validatetraked.trackedEntityInstances[0].trackedEntityInstance;
                        else
                            trackedInstDto.trackedEntityInstance = TrackeduidGeneratedDto.Codes[0].ToString();

                        trackedInstDto.trackedEntityType = objprogram.Trackedentitytype;
                        int ideventdate = Array.IndexOf(headers, objprogram.Incidentdatecolumm.ToUpperInvariant());
                        string eventdate = valores[ideventdate].ToString();
                        //trackedInstDto.orgUnit = ouFirts.id;
                        int ou = Array.IndexOf(headers, "OU_CODE");
                        var ouLine = Organisation.OrganisationUnits.Find(x => x.code == valores[ou].ToString());                                          
                        trackedInstDto.orgUnit = ouLine.id;
                        Console.Write("\nOU_CODE: " + ouLine.code.ToString());
                        List<Attribut> listAttribut = new List<Attribut>();
                        string enrollmentDatecolumm = "";
                        string incidentDatecolumm = "";
                        var id = Array.IndexOf(headers, objprogram.Enrollmentdatecolumm.ToUpperInvariant());
                        enrollmentDatecolumm = valores[id].ToString();
                        //enrollmentDatecolumm = valores[id].Split('/')[2].PadLeft(2, '0') + "-" + valores[id].Split('/')[1] + "-" + valores[id].Split('/')[0].PadLeft(2, '0');
                        var idi = Array.IndexOf(headers, objprogram.Incidentdatecolumm.ToUpperInvariant());
                        incidentDatecolumm = valores[idi].ToString();
                        //incidentDatecolumm = valores[idi].Split('/')[2].PadLeft(2, '0') + "-" + valores[idi].Split('/')[1] + "-" + valores[idi].Split('/')[0].PadLeft(2, '0');

                        foreach (Queries.DTOs.Attribute at in objprogram.Attribute)
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
                                        if (at.Id.Equals(objprogram.caseNum) && listtrackedInstDto.Count > 0)
                                        {
                                            foreach (TrackedEntityInstances tki in listtrackedInstDto)
                                                foreach (Attribut att in tki.attributes)
                                                    if (att.value.Equals(valores[idval]))
                                                        trackedEntityInstance = tki.trackedEntityInstance;
                                        }

                                        if (at.Name.Equals("Date of birth") || at.Name.Equals("Date of rash onset") || at.Name.Equals("WEA - Clasificación final"))
                                            attribut.value = valores[idval].ToString();// valores[idval].Split('/')[2].PadLeft(2, '0') + "-" + valores[idval].Split('/')[1] + "-" + valores[idval].Split('/')[0].PadLeft(2, '0');
                                        else
                                            attribut.value = valores[idval].ToString();
                                        if (at.Name.Equals("Is date of birth known"))
                                            attribut.value = valores[idval] == null ? "false" : "true";
                                        if (attribut != null)
                                        {
                                            listAttribut.Add(attribut);
                                        }
                                    }
                                }
                                catch (Exception e) {
                                    error = e.Message;
                                    Console.Write("\nError importación: " + error);
                                    EmailErrorImport();
                                }
                            }
                        }
                        var SequentialDto = await _dhis.GetSequential("1", commandGeneral.token);
                        trackedInstDto.attributes = listAttribut;
                        listtrackedInstDto.Add(trackedInstDto);
                        listtrackedInstDtoFull.Add(trackedInstDto);
                        trackedDto.trackedEntityInstances = listtrackedInstDto;
                        trackedDtos.trackedEntityInstances = listtrackedInstDtoFull;                        
                        Console.Write("\nCreando Tracked: " + trackedInstDto.attributes[11].value.ToString());
                        Enrollment enrollmentDto = new Enrollment();
                        Enrollment enrollmentFullDto = new Enrollment();
                        enrollmentDto.trackedEntityInstance = trackedInstDto.trackedEntityInstance;
                        enrollmentDto.program = objprogram.Programid;
                        enrollmentDto.status = "ACTIVE";
                        enrollmentDto.orgUnit = trackedInstDto.orgUnit;                        
                        enrollmentDto.enrollmentDate = enrollmentDatecolumm;
                        enrollmentDto.incidentDate = incidentDatecolumm;
                        enrollmentFullDto = enrollmentDto;
                        listEnrollment.Add(enrollmentDto);
                        listEnrollmentFull.Add(enrollmentDto);
                        enrollment.enrollments = listEnrollment;
                        enrollments.enrollments = listEnrollmentFull;
                        Console.Write("\nCreando Enrollment: " + enrollment.enrollments[0].status.ToString());
                        List<ProgramStageDataElement> dteObjarray = new List<ProgramStageDataElement>();

                        List<AddEventDto> listEvent = new List<AddEventDto>();
                        foreach (ProgramStage ps in objprogram.programStages)
                        {
                            //Start laboratory
                            if (ps.id.Equals("sNQqHHN5gi3"))
                            {
                                try
                                {

                                    for (int i = 0; i < RowFileLab.Count(); i++)
                                    {
                                        List<DataValue> listDataLabValue = new List<DataValue>();
                                        var dataValueLab = RowFileLab[i];
                                        if (dataValueLab[0].Equals(caseidvalue))
                                        {

                                            foreach (ProgramStageDataElement dtelab in ps.programStageDataElements)
                                            {
                                                try
                                                {
                                                    DataValue datavalue = new DataValue();
                                                    int idval = Array.IndexOf(headersLab, dtelab.dataElement.column.ToString().ToUpperInvariant());
                                                    if (idval >= 0)
                                                    {
                                                        datavalue.dataElement = dtelab.dataElement.id;
                                                        datavalue.value = dataValueLab[idval].ToString();
                                                        listDataLabValue.Add(datavalue);
                                                    }

                                                }
                                                catch (Exception e) { }
                                            }
                                            if (listDataLabValue.Count > 0)
                                            {
                                                string tkinsLab = "";
                                                if (trackedEntityInstance.Trim().Length == 0)
                                                    tkinsLab = trackedInstDto.trackedEntityInstance;
                                                else
                                                    tkinsLab = trackedEntityInstance;
                                                var storedBy = commandGeneral.UserLogin;
                                                UidGeneratedDto EventuidGeneratedDto = new UidGeneratedDto();
                                                EventuidGeneratedDto = await _dhis.GetUidGenerated("1", commandGeneral.token);
                                                string code = "";
                                                code = EventuidGeneratedDto.Codes[0].ToString();
                                                AddEventDto objEventDto = new AddEventDto
                                                {
                                                    programStage = ps.id,
                                                    program = objprogram.Programid,
                                                    orgUnit = trackedInstDto.orgUnit,
                                                    eventDate = eventdate,
                                                    status = "ACTIVE",
                                                    storedBy = storedBy,
                                                    trackedEntityInstance = tkinsLab,
                                                    event_ = code,
                                                    dataValues = listDataLabValue
                                                };

                                                listEvent.Add(objEventDto);                                               

                                            }

                                        }

                                    }

                                }

                                catch (Exception e) {
                                    error = e.Message;
                                    Console.Write("\nError importación (Laboratorio): " +  error.ToString());
                                    EmailErrorImport();
                                }
                            }
                            //end laboratory
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
                                            datavalue.dataElement = dte.dataElement.id;
                                            datavalue.value = valores[idval].ToString();
                                            //if(datavalue.dataElement == "w2GWdKFVkVk")  Validar que el formato de esta fecha sea yyyy-mm-dd
                                            listDataValue.Add(datavalue);
                                            cont = cont + 1;
                                        }
                                    }
                                    catch (Exception e) {
                                        contbad = contbad + 1;
                                        error = e.Message;
                                        Console.Write("\nError importación (Eventos): " + error.ToString());
                                        EmailErrorImport();
                                    }

                                }
                                string tkins = "";
                                if (listDataValue.Count > 0)
                                {
                                    try
                                    {
                                        if (trackedEntityInstance.Trim().Length == 0)
                                            tkins = trackedInstDto.trackedEntityInstance;
                                        else
                                            tkins = trackedEntityInstance;
                                    }
                                    catch (Exception e) {      
                                        error = e.Message;
                                        Console.Write("\nError importación (Eventos): " + error.ToString());
                                        EmailErrorImport();
                                    }

                                    var storedBy = commandGeneral.UserLogin;
                                    UidGeneratedDto EventuidGeneratedDto = new UidGeneratedDto();
                                    EventuidGeneratedDto = await _dhis.GetUidGenerated("1", commandGeneral.token);
                                    string code = "";
                                    code = EventuidGeneratedDto.Codes[0].ToString();
                                    AddEventDto objEventDto = new AddEventDto
                                    {
                                        programStage = ps.id,
                                        program = objprogram.Programid,
                                        orgUnit = trackedInstDto.orgUnit,
                                        eventDate = eventdate,
                                        status = "ACTIVE",
                                        storedBy = storedBy,
                                        trackedEntityInstance = tkins,
                                        event_ = code,
                                        dataValues = listDataValue
                                    };
                                    if (objEventDto.eventDate.Trim().Length > 0)
                                        listEvent.Add(objEventDto);

                                }
                               
                            }
                            catch (Exception e) {                               
                                error = e.Message;
                                Console.Write("\nError importación (Eventos): " + error.ToString());
                                EmailErrorImport();
                            }
                            //}
                        }
                        eventDto.events = listEvent;
                        Console.Write("\nCreando Events: " + listEvent.Count);
                        // Importante para envio en bloques de 100 o como se ajuste la configuracion
                        if (uploadBlock)
                        {
                            enrollmentFullDto.Eev = listEvent;
                            trackedInstDto.Eenr = listEnrollment;
                            var preuba = JsonConvert.SerializeObject(trackedDtos).Replace("Eenr", "enrollments").Replace("Eev", "events");
                        }
                    }

                    if (trackedDtos.trackedEntityInstances != null)
                    {

                        if (trackedDtos.trackedEntityInstances.Count >= SizeUpload)
                        {
                            AddTracketResultDto trakedResultDto = new AddTracketResultDto();
                            try
                            {
                                contBlock = contBlock + 1;
                                var resultDto = await _dhis.AddTracked(trackedDtos, commandGeneral.token);
                                Console.Write("\nImportacion Resultado Async:" + resultDto.Response.relativeNotifierEndpoint.ToString());
                                var res = await CheckImportTrackedAsync(resultDto.Response.relativeNotifierEndpoint, commandGeneral.token);
                                Console.Write("\nFin de Importacion en Bloque");
                            }
                            catch (Exception e) {                       
                                error = e.Message;
                                Console.Write("\nError importación (Guardado de Tracked): " +  error.ToString());
                                EmailErrorImport();
                            }

                            if (!uploadBlock)
                            {
                                AddEnrollmentResultDto enrollResultDto = new AddEnrollmentResultDto();
                                if (trakedResultDto.Status == "OK")
                                {
                                    try
                                    {
                                        enrollResultDto = await _dhis.AddEnrollment(enrollments, commandGeneral.token);
                                    }
                                    catch (Exception e) {
                                        error = e.Message;
                                        Console.Write("\nError importación (Guardado de Enrollments): " + error.ToString());
                                        EmailErrorImport();
                                    }

                                }
                                if (trakedResultDto.Status == "OK" && enrollResultDto.Status == "ACTIVE")
                                {
                                    try
                                    {
                                        var eventsResultDto = await _dhis.AddEvent(eventDto, commandGeneral.token);

                                    }
                                    catch (Exception e) {
                                        error = e.Message;
                                        Console.Write("\nError importación (Guardado de Eventos): " + error.ToString());
                                        EmailErrorImport();
                                    }
                                }
                            }
                            trackedDtos.trackedEntityInstances.Clear();
                        }
                    }

                    else
                    {
                        sendMailObj.SenEmailImport(_importSettings.Services[0].Server, "Sin datos para Importación", " No se pudo importar porque no existe Información para el rango de periodos seleccionados, favor revisar ", userSetting.email, _importSettings.Services[0].EmailFrom, _importSettings.Services[0].Pass, _importSettings.Services[0].Port, nameFile);
                    }




                }//cierre de while
                endWhile = true;
                if (trackedDtos.trackedEntityInstances != null)
                {
                    // Temporal para los archivos restantes de los bloques
                    if (trackedDtos.trackedEntityInstances.Count > 0)
                    {
                        AddTracketResultDto trakedResultDto = new AddTracketResultDto();
                        try
                        {
                            var resultDto = await _dhis.AddTracked(trackedDtos, commandGeneral.token);
                            Console.Write("\nImportacion Resultado Async:" + resultDto.Response.relativeNotifierEndpoint.ToString());
                            var res = await CheckImportTrackedAsync(resultDto.Response.relativeNotifierEndpoint, commandGeneral.token);
                            Console.Write("\nFin de Importacion");
                        }
                        catch (Exception e)
                        {
                            error = e.Message;
                            Console.Write("\nError importación: " +  error.ToString());
                            EmailErrorImport();
                        }
                    }
                }

                else
                {
                    sendMailObj.SenEmailImport(_importSettings.Services[0].Server, "Sin datos para Importación", " No se pudo importar porque no existe Información para el rango de periodos seleccionados, favor revisar ", userSetting.email, _importSettings.Services[0].EmailFrom, _importSettings.Services[0].Pass, _importSettings.Services[0].Port, nameFile);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                error = e.Message;
                Console.Write("\nError importación: " +  error.ToString());
                EmailErrorImport();
            }
        }

        public async Task<bool> CheckImportTrackedAsync(string task, string token)
        {
            try
            {
                Console.Write("\nInicio CheckImportTrackedAsync ");
                var response = await _dhis.GetStateTask(task.Replace("/api", ""), token);
                Console.Write("\nEstado de guardado Tracked : " + response.resultTasks[0].completed.ToString() + " detalles: " + response.resultTasks[0].message.ToString() );
                completed = response.resultTasks[0].completed;
                level = response.resultTasks[0].level;
                if (!completed )   
                {                   
                    await CheckImportTrackedAsync(task, token);
                    return false;
                }
                else
                {
                    var summary = await _dhis.GetSummaryImport(response.resultTasks[0].category, response.resultTasks[0].uid, token);                  
                    if (level == "ERROR")
                    {
                        error = response.resultTasks[0].message;
                        if (summary=="null")
                        {
                            summary = error;
                        }
                        Console.Write("\nError resultado de Importación : " + summary.ToString());
                        EmailErrorImport();
                    }
                    Console.Write("\nResultado de Importación : " + summary.ToString());
                    summaryImport.Add(summary);
                    blockSuccess = blockSuccess + 1;
                    Console.Write("\nFin CheckImportTrackedAsync ");
                    return true;
                }
                
            }
            catch (Exception e)
            {
                error = e.Message;
                Console.Write("\nError de resultado de Importación : " +  error.ToString());
                return false;
            }
        }



        public List<ArrayList> ReadCSV(historyCreateCommand command)
        {
            try
            {
                Console.Write("\nInicio de ReadCSV");
                commandGeneral = command;

                historyCreateCommand cmd = new historyCreateCommand();
                startDate = command.startdate + "-01-01";
                endDate = (command.enddate + 1) + "-01-01";

                if (command.CsvFile01 != null)
                {
                    readerLab = new StreamReader(command.CsvFile01.OpenReadStream());
                    headersLab = readerLab.ReadLine().Split(command.separator.ToString());
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
                    nameFileLab = commandGeneral.CsvFile01.FileName;
                    fileByteLabOrigin = new BinaryReader(commandGeneral.CsvFile01.OpenReadStream());
                    int f = (int)commandGeneral.CsvFile01.Length;
                    dataLabOrigin = fileByteLabOrigin.ReadBytes(f);
                    readerLab.Close();
                }

                string line;
                ArrayList list;
                //List<ArrayList> RowFile = new List<ArrayList>();
                reader = new StreamReader(command.CsvFile.OpenReadStream());

                headers = reader.ReadLine().Split(command.separator.ToString());
                headers = headers.Select(s => s.ToUpperInvariant()).ToArray();
                if (headers.Length <= 1) {
                    error = "El arhivo no tiene el formato solicitado, separacion por (,) ó (;)";
                    Console.Write("\nError de ReadCSV" + error.ToString());
                }
                while ((line = reader.ReadLine()) != null)
                {
                    contFiles = contFiles + 1;
                    var valores = line.Split(command.separator.ToString());
                    string[] LineFile = valores.Select(s => s.ToUpperInvariant()).ToArray();
                    list = new ArrayList(LineFile);
                    RowFile.Add(list);
                }

                fileByteOrigin = new BinaryReader(commandGeneral.CsvFile.OpenReadStream());
                int i = (int)commandGeneral.CsvFile.Length;
                dataOrigin = fileByteOrigin.ReadBytes(i);
                if (dataLabOrigin == null)
                {
                    dataLabOrigin = dataOrigin;
                }
                reader.Close();
                Console.Write("\nFin de ReadCSV");
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                error = e.Message;
                Console.Write("\nError de ReadCSV : " +  error.ToString());
                EmailErrorImport();
            }
            return RowFile;
        }

        public List<ArrayList> ReadXLSX(historyCreateCommand command)
        {
            try
            {
                Console.Write("\nInicio de ReadXLSX");
                commandGeneral = command;
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
                                    string rec = cellInd.Substring(0, 10);
                                    string date = Convert.ToDateTime(rec).ToString("yyyy-MM-dd");
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
                                string rec = cellInd.Substring(0, 10);
                                string date = Convert.ToDateTime(rec).ToString("yyyy-MM-dd");
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


                fileByteOrigin = new BinaryReader(commandGeneral.CsvFile.OpenReadStream());
                int k = (int)commandGeneral.CsvFile.Length;
                dataOrigin = fileByteOrigin.ReadBytes(k);
                dataLabOrigin = dataOrigin;
                readerExcel.Close();
                Console.Write("\nFin de ReadXLSX");
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                error = e.Message;
                Console.Write("\nError de ReadXLSX : " + error.ToString());
                EmailErrorImport();
            }
            return RowFile;

        }

        public List<ArrayList> ReadXLS(historyCreateCommand command)
        {
            try
            {
                Console.Write("\nInicio de ReadXLS");
                commandGeneral = command;
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
                                        string rec = cellInd.Substring(0, 10);
                                        string date = Convert.ToDateTime(rec).ToString("yyyy-MM-dd");
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
                                    string rec = cellInd.Substring(0, 10);
                                    string date = Convert.ToDateTime(rec).ToString("yyyy-MM-dd");
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

                fileByteOrigin = new BinaryReader(commandGeneral.CsvFile.OpenReadStream());
                int k = (int)commandGeneral.CsvFile.Length;
                dataOrigin = fileByteOrigin.ReadBytes(k);
                dataLabOrigin = dataOrigin;
                readerExcel.Close();
                Console.Write("\nFin de ReadXLS");
                return RowFile;
            }
            catch (Exception e)
            {
                error = e.Message;
                Console.Write("\nError de ReadXLS: " + error.ToString());
                EmailErrorImport();
            }

            return RowFile;
        }

        public async Task Handle(historyCreateCommand command, CancellationToken cancellation)
        {
            try
            {
                commandGeneral = command;
                command.reponse = response;
                userSetting = await loginQueyService.GetUserSetting(commandGeneral.token);
                var ExternalImportDataApp = await _dhis.GetAllProgramAsync(commandGeneral.token);
                objprogram = ExternalImportDataApp.Programs.Where(a => a.Programid.Equals(commandGeneral.Programsid)).FirstOrDefault();                
                Organisation = await _dhis.GetAllOrganisation(commandGeneral.token); //crear el objeto de tipo AddEventDto
                var contentOrg = JsonConvert.SerializeObject(Organisation);
                var _set = _importSettings;
                uploadBlock = _set.Services[0].Block && !_set.Services[0].Individual;
                nameFile = command.CsvFile.FileName;

                string fileExtension = Path.GetExtension(commandGeneral.CsvFile.FileName);
                if (fileExtension == ".csv")
                {
                    RowFile = ReadCSV(commandGeneral);
                }
                if (fileExtension == ".xlsx")
                {
                    RowFile = ReadXLSX(commandGeneral);
                }
                if (fileExtension == ".xls")
                {
                    RowFile = ReadXLS(commandGeneral);
                }
                if (fileExtension != ".csv" || fileExtension != ".xlsx" || fileExtension != ".xls") {
                    error = "El tipo de archivo" + Path.GetExtension(commandGeneral.CsvFile.FileName) +"  no es compatible con los archivos aceptados (*.csv (separado por , ó ;), *.xls y *.xlsx)" ;
                }

                //unidades organizativas              
                OrganisationUnit ounitsFirst = new OrganisationUnit();
                int colounitsFirst = Array.IndexOf(headers, objprogram.Orgunitcolumm.ToUpperInvariant());
                if (colounitsFirst == -1)
                {
                    if(String.IsNullOrEmpty(error))
                        error = "El archivo no tiene la estructura correcta, posiblemente no tiene la Unidad Organizativa";
                }
                var Firtsline = RowFile[0];
                ounitvalueFirst = Firtsline[colounitsFirst].ToString();

                ouFirts = Organisation.OrganisationUnits.Find(x => x.code == Firtsline[colounitsFirst].ToString());
                oupath = ouFirts.path.Split("/")[2];
                var oupathFull = await _dhis.GetOrganisationUnit(commandGeneral.token, oupath); //crear el objeto de tipo AddEventDto
                country = oupathFull.name;
                backgroundTask.StartAsync(OrchestratorAsync(oupath, startDate, endDate, objprogram.Programid, command.token, RowFile));
                //return "start";               
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                if (String.IsNullOrEmpty(error))
                {
                    error = e.Message;
                }
                Console.Write("\nError Handle : " +  error.ToString());
                command.reponse = error;
                EmailErrorImport();
            }
        }

        public void SaveSummaryImport(string userLogin, string program)
        {
            try
            {
                Console.Write("\nInicio de guardado Sumamry DB ");
                var _cnx = _conexionInt;
                using (var cn = new NpgsqlConnection(_cnx.ConexionDatabase))
                {
                    cn.Open();

                    using (var cmd = new NpgsqlCommand("INSERT INTO history (programsid, jsonset, jsonresponse, state, userlogin, fecha, file, deleted, uploads, namefile, country, file1, namefile1) VALUES( @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9,@p10, @p11,@p12,@p13,@p14  )", cn))
                    {

                        cmd.Parameters.Clear();
                        cmd.Parameters.Add("@p2", NpgsqlDbType.Text).Value = program;
                        cmd.Parameters.Add("@p3", NpgsqlDbType.Text).Value = JsonConvert.SerializeObject(summaryImport);
                        cmd.Parameters.Add("@p4", NpgsqlDbType.Text).Value = "Procesado";
                        cmd.Parameters.Add("@p5", NpgsqlDbType.Boolean).Value = true;
                        cmd.Parameters.Add("@p6", NpgsqlDbType.Varchar).Value = userLogin;
                        cmd.Parameters.Add("@p7", NpgsqlDbType.Timestamp).Value = DateTime.Now;
                        cmd.Parameters.Add("@p8", NpgsqlDbType.Bytea).Value = dataOrigin;
                        cmd.Parameters.Add("@p9", NpgsqlDbType.Integer).Value = 0;
                        cmd.Parameters.Add("@p10", NpgsqlDbType.Integer).Value = 0;
                        cmd.Parameters.Add("@p11", NpgsqlDbType.Varchar).Value = nameFile;
                        cmd.Parameters.Add("@p12", NpgsqlDbType.Varchar).Value = country;
                        cmd.Parameters.Add("@p13", NpgsqlDbType.Bytea).Value = dataLabOrigin;
                        cmd.Parameters.Add("@p14", NpgsqlDbType.Text).Value = nameFileLab;

                        var result = cmd.ExecuteNonQuery();
                        state = 5;
                        Console.Write("Fin de guardado Summary DB");
                    }
                    cn.Close();
                }
            }
            catch (Exception e)
            {
                error = e.Message;
                Console.Write("\nError Guardado Summry DB : " +  error.ToString());
                EmailErrorImport();
            }
        }
        public void EmailErrorImport() {
            sendMailObj.SenEmailImport(_importSettings.Services[0].Server, "Error en la Importación", ": No se logro Importar  \n *** Importante ***\nPaso 1: Cargue del Archivo \nPaso 2: Borrado de Events  \nPaso 3: Borrado de Enrollments \nPaso 4: Importación de la Data (Tracked= Registro de la persona,  Enrollment= Registro de la persona al progama y Events= Datos del registro) \nPaso 5: Guardado del Resumen de la Importación \nPaso 6: Notificación por Email de la Importación \n\nA continuación, el error en detalle: " + error + "\n El paso donde ocurrio el error: " + state, userSetting.email, _importSettings.Services[0].EmailFrom, _importSettings.Services[0].Pass, _importSettings.Services[0].Port, nameFile);
        }
        public async Task Handle(historyUpdateCommand command, CancellationToken cancellation)
        {
            await Task.Run(async () =>
            {
                var history = await _context.history.FindAsync(command.Id);
                history.jsonresponse = command.JsonResponse;
                history.state = command.State;

                await _context.SaveChangesAsync();
            });
        }
    }
}