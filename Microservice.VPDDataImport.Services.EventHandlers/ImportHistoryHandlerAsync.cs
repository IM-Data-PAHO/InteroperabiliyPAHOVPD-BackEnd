using Microservice.VPDDataImport.Domain;
using Microservice.VPDDataImport.Persistence.Database;
using Microservice.VPDDataImport.Services.EventHandlers.Commands;
using Microservice.VPDDataImport.Services.Queries;
using Microservice.VPDDataImport.Services.Queries.DTOs;
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
using System.Globalization;

namespace Microservice.VPDDataImport.Services.EventHandlers
{
    public class ImportHistoryHandlerAsync : INotificationHandler<historyCreateCommand>,
         INotificationHandler<historyUpdateCommand>

    // Atributos de la clase 
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
        public List<string> summaryImportW = new List<string>();
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
        public bool completed = false;
        public DhisProgramDto ExternalImportDataApp;
        public int nodate = 0;
        public string summaryNodata = "";
        public List<SequentialDto> SequentialDto;
        public string statusSummary;
        public string errorSummary;
        public ImportHistoryHandlerAsync(ApplicationDbContext context, IDhisQueryService dhis)
        {
            _context = context;
            _dhis = dhis;
        }


        /// <summary>
        /// Retorna configuraciones que se declararon en el AppSettings, en este caso para la importación
        /// </summary>
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
                        SubjectSomeError = Convert.ToString(s.GetSection("subjectSomeError").Value),
                        BodySomeError = Convert.ToString(s.GetSection("bodySomeError").Value),
                        SubjectError = Convert.ToString(s.GetSection("subjectError").Value),
                        BodyError = Convert.ToString(s.GetSection("bodyError").Value),
                        SubjectEmpty = Convert.ToString(s.GetSection("subjectEmpty").Value),
                        BodyEmpty = Convert.ToString(s.GetSection("bodyEmpty").Value),
                        TitleError = Convert.ToString(s.GetSection("titleError").Value),
                        Pass = Convert.ToString(s.GetSection("pass").Value),
                        Port = Convert.ToString(s.GetSection("port").Value)
                    }).ToList()
                };

            }
        }


        /// <summary>
        /// Retorna la cadena de conexión para la base de datos, configurada en el AppSettings
        /// </summary>
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

        /// <summary>
        ///Método que orquesta todo el proceso,esta divido por pasos (todas las etapas de la importación (5 pasos), no permite iniciar un nuevo paso hasta el anterior este terminado.
        /// </summary>
        /// <param name="oupath">Unidad Organizativa Padre</param>
        /// <param name="startDate">Fecha inicial</param>
        /// <param name="endDate">Fecha final</param>
        /// <param name="program">Programa</param>
        /// <param name="token">Token</param>
        /// <param name="RowFile">Objeto iterable del arhivo obligatorio</param>
        /// <returns>No retorna nada</returns>
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
                        // Paso 1: Consulta y borrado de los eventos                                                 
                        case 1:
                            step = 1;
                            TaskResult = await CleanEventsAsync(oupath, program, startDate, endDate, token);
                            break;
                        //Paso 2: Consulta y borrado de los enrollments
                        case 2:
                            step = 2;
                            await CleanEnrollmentsAsync(program, oupath, startDate, endDate, token);
                            break;
                        //Paso 3: Construccón del JSON para la importación de la data
                        case 3:
                            step = 3;
                            await ImportDataAsync(RowFile);
                            if (endWhile)
                            {
                                state = 4;
                            }
                            break;
                        //Paso 4: Guardado del summary (resultado de la importación) en la base de datos
                        case 4:
                            SaveSummaryImport(commandGeneral.UserLogin, objprogram.Programname);
                            break;
                        //Paso 5: Envio de email, reportando si hubo o no errores durante la importación
                        case 5:
                            //string errorSummary = await ReadErrorSummaryAsync(summaryImportW, token);
                            error += !String.IsNullOrEmpty(errorSummary) ? error + "\n" + errorSummary : "";
                            string body = _importSettings.Services[0].Body;
                            string subject = _importSettings.Services[0].Subject;
                            if (nodate > 0)
                            {
                                subject = _importSettings.Services[0].SubjectEmpty;
                                body = _importSettings.Services[0].BodyEmpty;
                            }

                            if ((level != "ERROR" || level != null) && nodate == 0)
                            {
                                subject = !String.IsNullOrEmpty(error) ? _importSettings.Services[0].SubjectSomeError : subject;
                                body = !String.IsNullOrEmpty(error) ? _importSettings.Services[0].BodySomeError : body;
                                error = !String.IsNullOrEmpty(error) ? _importSettings.Services[0].TitleError + "\n" + error : "";
                            }
                            if (level == "ERROR" && nodate == 0)
                            {
                                subject = !String.IsNullOrEmpty(error) ? _importSettings.Services[0].SubjectError : subject;
                                body = !String.IsNullOrEmpty(error) ? _importSettings.Services[0].BodyError : body;
                                error = !String.IsNullOrEmpty(error) ? _importSettings.Services[0].TitleError + "\n" + error : "";
                            }
                            sendMailObj.SenEmailImport(_importSettings.Services[0].Server, subject, body + error, userSetting.email, _importSettings.Services[0].EmailFrom, _importSettings.Services[0].Pass, _importSettings.Services[0].Port, "El ó los archivo(s): " + nameFile + " " + nameFileLab);
                            state = 6;
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
                error += "\n" + e.Message;
                commandGeneral.reponse = e.Message;
                Console.WriteLine("{0} Exception caught.", e);
                state = 6;
                EmailErrorImport();

            }
        }
        /// <summary>
        /// Permite determinar si la tarea asincrona se ejecuto para cambiar el estado y continuar con el siguiente paso del método Orquestador
        /// </summary>
        /// <param name="state"> Estado</param>
        /// <param name="step">Siguiente paso</param>
        /// <param name="task">url de la tarea asincrona</param>
        /// <param name="token">token</param>
        /// <param name="numImport"></param>
        /// <returns>bool (si se termino ó no la tarea-9</returns>
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
                    if (level == "ERROR")
                    {
                        error = response.resultTasks[0].message;
                        Console.Write("\nError Eliminación de Eventos : " + error.ToString());
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
                error += "\n" + e.Message;
                return false;
            }
        }

        /// <summary>
        /// Permite determinar si existen eventos y posterior a esto eliminarlos, En caso que no retorne data cambia el state al siguiente paso
        /// </summary>
        /// <param name="oupath">Unidad organitizativa padre</param>
        /// <param name="program">Programa</param>
        /// <param name="startDate">Fecha inicial</param>
        /// <param name="endDate">Fecha final</param>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna la url de la tarea asincrona para validar la eliminación de los eventos</returns>
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
                error += "\n" + e.Message;
                Console.Write("\nError de Eliminación de Eventos; ", error);
                return "";
            }
        }

        //
        /// <summary>
        /// Permite determinar si existen enrrollments y posterior a esto eliminarlos, en caso que no retorne data cambia el state al siguiente paso
        /// </summary>
        /// <param name="program">Programa</param>
        /// <param name="oupath">Unidad organizativa padre</param>
        /// <param name="startDate">Fecha inicial</param>
        /// <param name="endDate">Fecha final</param>
        /// <param name="token">Token de autenticación</param>
        /// <returns>No retorna nada</returns>
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
                    else
                    {
                        error = dropEnrrollments.message;
                        Console.Write("\nError de Eliminación de Enrollments" + error.ToString());
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
                error += "\n" + e.Message;
                Console.Write("\nError Eliminación de Enrollments: ", error);
            }
        }


        /// <summary>
        /// Importa la data, recibe el objeto iterable que corresponde al 1er arhivo de Casos (obligatorio), este proceso organiza el archivo JSON, para posterior enviar la data a guardar
        /// </summary>
        /// <param name="RowFile">Obejto iterable de la 1era hoja ó arhivo</param>
        /// <returns>No retorna nada</returns>
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
                string caseidvalue = "";
                string eventdate = "";
                string dtenrollmentval = "";

                int total = RowFile.Count();
                SequentialDto = await _dhis.GetSequential(total.ToString(), commandGeneral.token);
                //Leemos cada una de las lineas del objeto iterable, el cual va iterando cada linea
                while (cic < RowFile.Count)
                {
                    contFiles = contFiles + 1;
                    var valores = RowFile[cic];

                    Console.Write("Ciclos: " + cic.ToString());
                    cic++;
                    var enrollmentId = Array.IndexOf(headers, objprogram.Enrollmentdatecolumm.ToUpperInvariant());
                   
                    try
                        {
                        if (enrollmentId >= 0)
                        {
                            if (!String.IsNullOrEmpty(valores[enrollmentId].ToString()))
                            {
                                string[] partdate = valores[enrollmentId].ToString().Split("/");

                                if (partdate.Length == 3 && partdate[2].Length == 4)
                                {
                                    if (Convert.ToInt32(partdate[1]) > 12 && Convert.ToInt32(partdate[0]) > 31)
                                    {
                                        dtenrollmentval = partdate[2] + "-" + partdate[0] + "-" + partdate[1];
                                    }
                                    else
                                    {
                                        dtenrollmentval = partdate[2] + "-" + partdate[1] + "-" + partdate[0];
                                    }
                                }
                                else
                                {
                                    dtenrollmentval = valores[enrollmentId].ToString().Trim();
                                }
                            }
                            //dtRashOnval = !String.IsNullOrWhiteSpace(valores[dtRashOn].ToString()) ? Convert.ToDateTime(valores[dtRashOn].ToString().Trim()).ToString("yyyy-MM-dd") : valores[dtRashOn].ToString().Trim();
                            //valores[dtRashOn] = dtRashOnval;
                        }
                        else
                        {
                            error += "\nNo existe información para dtRashOn";
                        }
                    }
                    catch (Exception e)
                    {
                        error += "\n" + e.Message;

                    }
                    int dty = 0;
                    try
                    {
                        dty = Convert.ToInt32(Convert.ToDateTime(dtenrollmentval).Year);
                    }
                    catch (Exception e)
                    {
                        error = e.Message;
                        Console.WriteLine("{0} Exception caught.", e);
                    }
                    if (dty == commandGeneral.startdate || dty == commandGeneral.enddate)
                    {
                        List<TrackedEntityInstances> listtrackedInstDto = new List<TrackedEntityInstances>();
                        List<Enrollment> listEnrollment = new List<Enrollment>();

                        AddEnrollmentDto enrollment = new AddEnrollmentDto();
                        string trackedEntityInstance = "";
                        //Genero un uid 
                        var TrackeduidGeneratedDto = await _dhis.GetUidGenerated("1", commandGeneral.token);

                        cont = cont + 1;
                        TrackedEntityInstances trackedInstDto = new TrackedEntityInstances();

                        int caseid = Array.IndexOf(headers, objprogram.caseidcolumm.ToUpperInvariant());
                        if (caseid >= 0)
                        {
                            caseidvalue = valores[caseid].ToString();
                        }
                        else
                        {
                            error += "\nNo existe información para CASE_ID";
                        }
                        Console.Write("\nCASE ID: " + caseidvalue.ToString());
                        int ou = Array.IndexOf(headers, objprogram.Orgunitcolumm.ToUpperInvariant());
                        if (ou >= 0)
                        {
                            var ouLine = Organisation.OrganisationUnits.Find(x => x.code == valores[ou].ToString().Trim());
                            if (ouLine != null)
                            {
                                trackedInstDto.orgUnit = ouLine.id;
                                var codeCaseID = objprogram.Attribute.Find(x => x.Column == objprogram.caseidcolumm.ToUpperInvariant());
                                Organisation.OrganisationUnits.Find(x => x.code == valores[ou].ToString().Trim());
                                var validatetraked = await _dhis.GetTracked(caseidvalue, ouLine.id, commandGeneral.token, codeCaseID.Id);
                                if (validatetraked.trackedEntityInstances.Count > 0)
                                    trackedInstDto.trackedEntityInstance = validatetraked.trackedEntityInstances[0].trackedEntityInstance;
                                else
                                    trackedInstDto.trackedEntityInstance = TrackeduidGeneratedDto.Codes[0].ToString().Trim();

                                trackedInstDto.trackedEntityType = objprogram.Trackedentitytype;

                                Console.Write("\nOU_CODE: " + ouLine.code.ToString());
                            }
                            else
                            {
                                error += "\nNo existe información para OU_CODE " + valores[ou].ToString() + " CASE_ID: " + caseidvalue;
                            }
                        }
                        else
                        {
                            error += "\nNo existe información para OU_CODE " + valores[ou].ToString() + " CASE_ID: " + caseidvalue;
                        }

                        int ideventdate = Array.IndexOf(headers, objprogram.Incidentdatecolumm.ToUpperInvariant());
                        try
                        {
                            if (ideventdate >= 0)
                            {
                                if (!String.IsNullOrEmpty(valores[ideventdate].ToString()))
                                {
                                    //{
                                    //    string[] partdate = valores[ideventdate].ToString().Trim().Split("/");
                                    //    valores[ideventdate] = partdate.Length == 3 ? partdate[2] + "-" + partdate[1] + "-" + partdate[0] : valores[ideventdate].ToString().Trim();
                                    //}

                                    string[] partdate = valores[ideventdate].ToString().Split("/");

                                    if (partdate.Length == 3 && partdate[2].Length == 4)
                                    {
                                        if (Convert.ToInt32(partdate[1]) > 12 && Convert.ToInt32(partdate[0]) > 31)
                                        {
                                            eventdate = partdate[2] + "-" + partdate[0] + "-" + partdate[1];
                                        }
                                        else
                                        {
                                            eventdate = partdate[2] + "-" + partdate[1] + "-" + partdate[0];
                                        }
                                    }
                                    else
                                    {
                                        eventdate = valores[ideventdate].ToString().Trim();
                                    }
                                    //eventdate = !String.IsNullOrWhiteSpace(valores[ideventdate].ToString().Trim()) ? Convert.ToDateTime(valores[ideventdate].ToString().Trim()).ToString("yyyy-MM-dd") : valores[ideventdate].ToString().Trim();

                                }
                            }
                            else
                            {
                                error += "\nNo existe información para " + objprogram.Incidentdatecolumm.ToUpperInvariant();
                            }
                        }
                        catch (Exception e)
                        {
                            error += "\n" + e.Message;
                        }

                        List<Attribut> listAttribut = new List<Attribut>();
                        string enrollmentDatecolumm = "";
                        string incidentDatecolumm = "";
                        var id = Array.IndexOf(headers, objprogram.Enrollmentdatecolumm.ToUpperInvariant());
                        try
                        {
                            if (id >= 0)
                            {
                                if (!String.IsNullOrEmpty(valores[id].ToString()))
                                {
                                    string[] partdate = valores[id].ToString().Split("/");
                                    if (partdate.Length == 3 && partdate[2].Length == 4)
                                    {
                                        if (Convert.ToInt32(partdate[1]) > 12 && Convert.ToInt32(partdate[0]) > 31)
                                        {
                                            enrollmentDatecolumm = partdate[2] + "-" + partdate[0] + "-" + partdate[1];
                                        }
                                        else
                                        {
                                            enrollmentDatecolumm = partdate[2] + "-" + partdate[1] + "-" + partdate[0];
                                        }
                                    }
                                    else
                                    {
                                        enrollmentDatecolumm = valores[id].ToString().Trim();
                                    }

                                    //string[] partdate = valores[id].ToString().Trim().Split("/");
                                    //valores[id] = partdate.Length == 3 && partdate[2].Length==4 ? partdate[2] + "-" + partdate[1] + "-" + partdate[0] : valores[id].ToString().Trim();
                                }
                                // enrollmentDatecolumm = !String.IsNullOrWhiteSpace(valores[id].ToString().Trim()) ? Convert.ToDateTime(valores[id].ToString().Trim()).ToString("yyyy-MM-dd") : valores[id].ToString().Trim();
                            }
                            else
                            {
                                error += "\nNo existe información para " + objprogram.Enrollmentdatecolumm.ToUpperInvariant();
                            }
                        }
                        catch (Exception e)
                        {
                            error += "\n" + e.Message;
                        }
                        var idi = Array.IndexOf(headers, objprogram.Incidentdatecolumm.ToUpperInvariant());
                        try
                        {
                            if (idi >= 0)
                            {
                                if (!String.IsNullOrEmpty(valores[idi].ToString()))
                                {
                                    string[] partdate = valores[idi].ToString().Split("/");
                                    if (partdate.Length == 3 && partdate[2].Length == 4)
                                    {
                                        if (Convert.ToInt32(partdate[1]) > 12 && Convert.ToInt32(partdate[0]) > 31)
                                        {
                                            incidentDatecolumm = partdate[2] + "-" + partdate[0] + "-" + partdate[1];
                                        }
                                        else
                                        {
                                            incidentDatecolumm = partdate[2] + "-" + partdate[1] + "-" + partdate[0];
                                        }
                                        //string[] partdate = valores[idi].ToString().Trim().Split("/");
                                        //valores[idi] = partdate.Length == 3 ? partdate[2] + "-" + partdate[1] + "-" + partdate[0] : valores[idi].ToString().Trim();
                                    }
                                    else
                                    {
                                        incidentDatecolumm = valores[idi].ToString().Trim();
                                    }
                                    // incidentDatecolumm = !String.IsNullOrWhiteSpace(valores[idi].ToString().Trim()) ? Convert.ToDateTime(valores[idi].ToString().Trim()).ToString("yyyy-MM-dd") : valores[idi].ToString().Trim();
                                }
                            }
                            else
                            {
                                error += "\nNo existe información para " + objprogram.Incidentdatecolumm.ToUpperInvariant();
                            }
                        }
                        catch (Exception e)
                        {
                            error += "\n" + e.Message;
                        }
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
                                        if (at.valueType == "DATE" && !String.IsNullOrWhiteSpace(valores[idval].ToString().Trim()))
                                        {

                                            // valores[idval] = Convert.ToDateTime(valores[idval].ToString().Trim()).ToString("yyyy-MM-dd");
                                            string[] partdate = valores[idval].ToString().Split("/");
                                            if (partdate.Length == 3 && partdate[2].Length == 4)
                                            {
                                                if (Convert.ToInt32(partdate[1]) > 12 && Convert.ToInt32(partdate[0]) > 31)
                                                {
                                                    valores[idval] = partdate[2] + "-" + partdate[0] + "-" + partdate[1];
                                                }
                                                else
                                                {
                                                    valores[idval] = partdate[2] + "-" + partdate[1] + "-" + partdate[0];
                                                }
                                            }

                                        }
                                        if (at.Id.Equals(objprogram.caseNum) && listtrackedInstDto.Count > 0)
                                        {
                                            foreach (TrackedEntityInstances tki in listtrackedInstDto)
                                                foreach (Attribut att in tki.attributes)
                                                    if (att.value.Equals(valores[idval]))
                                                        trackedEntityInstance = tki.trackedEntityInstance;
                                        }

                                        if (at.Name.Equals("Date of birth") || at.Name.Equals("Date of rash onset") || at.Name.Equals("WEA - Clasificación final"))
                                        {
                                            Console.Write("\ncumple: " + valores[idval].ToString().Trim());
                                            attribut.value = valores[idval].ToString().Trim();
                                        }
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
                                catch (Exception e)
                                {
                                    error += "\n" + e.Message;
                                    Console.Write("\nError importación: " + error);
                                }
                            }
                        }

                        Attribut attributSq = new Attribut();
                        attributSq.attribute = "mxKJ869xJOd";
                        attributSq.value = SequentialDto[cic - 1].value;
                        listAttribut.Add(attributSq);
                        trackedInstDto.attributes = listAttribut;
                        listtrackedInstDto.Add(trackedInstDto);
                        listtrackedInstDtoFull.Add(trackedInstDto);
                        trackedDto.trackedEntityInstances = listtrackedInstDto;
                        trackedDtos.trackedEntityInstances = listtrackedInstDtoFull;
                        Console.Write("\nCreando Tracked: ");
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
                            //Inicio de la construcción de la opción de Laboratorio
                            if (ps.name.Trim().Equals("Laboratory"))
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
                                                    int idval = Array.IndexOf(headersLab, dtelab.dataElement.column.ToString().Trim().ToUpperInvariant());
                                                    if (idval >= 0)
                                                    {
                                                        if (dtelab.dataElement.valueType == "DATE" && !String.IsNullOrWhiteSpace(dataValueLab[idval].ToString()))
                                                        {
                                                            string[] partdate = dataValueLab[idval].ToString().Split("/");
                                                            if (partdate.Length == 3 && partdate[2].Length == 4)
                                                            {
                                                                if (Convert.ToInt32(partdate[1]) > 12 && Convert.ToInt32(partdate[0]) > 31)
                                                                {
                                                                    dataValueLab[idval] = partdate[2] + "-" + partdate[0] + "-" + partdate[1];
                                                                }
                                                                else
                                                                {
                                                                    dataValueLab[idval] = partdate[2] + "-" + partdate[1] + "-" + partdate[0];
                                                                }

                                                            }
                                                            // dataValueLab[idval] = Convert.ToDateTime(dataValueLab[idval].ToString().Trim()).ToString("yyyy-MM-dd");
                                                        }
                                                        datavalue.dataElement = dtelab.dataElement.id;
                                                        datavalue.value = dataValueLab[idval].ToString().Trim();
                                                        listDataLabValue.Add(datavalue);
                                                    }

                                                }
                                                catch (Exception e)
                                                {
                                                    error += "\n" + e.Message;
                                                    Console.Write("\nError importación: " + error);
                                                }
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

                                catch (Exception e)
                                {
                                    error += "\n" + e.Message;
                                    Console.Write("\nError importación (Laboratorio): " + error.ToString());
                                }
                            }
                            //Fin de la construcción de la opción de Laboratorio
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
                                            if (dte.dataElement.valueType == "DATE" && !String.IsNullOrWhiteSpace(valores[idval].ToString().Trim()))
                                            {
                                                string[] partdate = valores[idval].ToString().Split("/");
                                                if (partdate.Length == 3 && partdate[2].Length == 4)
                                                {
                                                    if (Convert.ToInt32(partdate[1]) > 12 && Convert.ToInt32(partdate[0]) > 31)
                                                    {
                                                        valores[idval] = partdate[2] + "-" + partdate[0] + "-" + partdate[1];
                                                    }
                                                    else
                                                    {
                                                        valores[idval] = partdate[2] + "-" + partdate[1] + "-" + partdate[0];
                                                    }

                                                }
                                                //valores[idval] = Convert.ToDateTime(valores[idval].ToString().Trim()).ToString("yyyy-MM-dd");
                                            }
                                            datavalue.dataElement = dte.dataElement.id;
                                            datavalue.value = valores[idval].ToString().Trim();
                                            listDataValue.Add(datavalue);
                                            cont = cont + 1;
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        contbad = contbad + 1;
                                        error += "\n" + e.Message;
                                        Console.Write("\nError importación (Eventos): " + error.ToString());
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
                                    catch (Exception e)
                                    {
                                        error += "\n" + e.Message;
                                        Console.Write("\nError importación (Eventos): " + error.ToString());
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
                            catch (Exception e)
                            {
                                error += "\n" + e.Message;
                                Console.Write("\nError importación (Eventos): " + error.ToString());
                            }

                        }
                        eventDto.events = listEvent;
                        Console.Write("\nCreando Events: " + listEvent.Count);
                        // Importante para envio en bloques de 100 ó como se ajuste la configuración
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
                                Console.Write("\nImportacion Resultado Async:" + resultDto.Response.relativeNotifierEndpoint.ToString().Trim());
                                var res = await CheckImportTrackedAsync(resultDto.Response.relativeNotifierEndpoint, commandGeneral.token);
                                Console.Write("\nFin de Importacion en Bloque");
                            }
                            catch (Exception e)
                            {
                                error += "\n" + e.Message;
                                Console.Write("\nError importación (Guardado de Tracked): " + error.ToString());
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
                                    catch (Exception e)
                                    {
                                        error += "\n" + e.Message;
                                        Console.Write("\nError importación (Guardado de Enrollments): " + error.ToString());
                                    }

                                }
                                if (trakedResultDto.Status == "OK" && enrollResultDto.Status == "ACTIVE")
                                {
                                    try
                                    {
                                        var eventsResultDto = await _dhis.AddEvent(eventDto, commandGeneral.token);

                                    }
                                    catch (Exception e)
                                    {
                                        error += "\n" + e.Message;
                                        Console.Write("\nError importación (Guardado de Eventos): " + error.ToString());
                                    }
                                }
                            }
                            trackedDtos.trackedEntityInstances.Clear();
                        }
                    }

                    else
                    {
                        nodate += 1;
                        summaryNodata = String.IsNullOrEmpty(summaryNodata) ? "Sin data para importar" : "";
                        if (!String.IsNullOrEmpty(summaryNodata))
                        {
                            summaryImport.Add(summaryNodata);
                        }

                    }

                }//cierre de while
                endWhile = true;
                if (trackedDtos.trackedEntityInstances != null)
                {
                    // Para los archivos restantes de los bloques
                    if (trackedDtos.trackedEntityInstances.Count > 0)
                    {
                        AddTracketResultDto trakedResultDto = new AddTracketResultDto();
                        try
                        {
                            var resultDto = await _dhis.AddTracked(trackedDtos, commandGeneral.token);
                            Console.Write("\nImportacion Resultado Async:" + resultDto.Response.relativeNotifierEndpoint.ToString().Trim());
                            var res = await CheckImportTrackedAsync(resultDto.Response.relativeNotifierEndpoint, commandGeneral.token);
                            Console.Write("\nFin de Importacion");
                        }
                        catch (Exception e)
                        {
                            error += "\n" + e.Message;
                            Console.Write("\nError importación: " + error.ToString());
                        }
                    }
                }

                else
                {
                    nodate += 1;
                    summaryNodata = String.IsNullOrEmpty(summaryNodata) ? "Sin data para importar" : "";
                    if (!String.IsNullOrEmpty(summaryNodata))
                    {
                        summaryImport.Add(summaryNodata);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("{0} Exception caught.", e);
                error += "\n" + e.Message;
                Console.Write("\nError importación: " + error.ToString());
                state = 5;
            }
        }

        /// <summary>
        /// Revisa el estado de la tarea asincrona del guardado de la data, verifica si la tarea término y si tiene  ó no errores
        /// y genera el summary para posterior guardado en la base de datos y envio de errroes al email
        /// </summary>
        /// <param name="task">Url de la tarea asincrona</param>
        /// <param name="token">Token de autenticación</param>
        /// <returns>Retorna un bool con el resultado de la tarea (terminada ó no/ con ó sin errores)</returns>
        public async Task<bool> CheckImportTrackedAsync(string task, string token)
        {
            try
            {
                Console.Write("\nInicio CheckImportTrackedAsync ");
                var response = await _dhis.GetStateTask(task.Replace("/api", ""), token);
                Console.Write("\nEstado de guardado Tracked : " + response.resultTasks[0].completed.ToString() + " detalles: " + response.resultTasks[0].message.ToString());
                completed = response.resultTasks[0].completed;
                level = response.resultTasks[0].level;
                if (!completed)
                {
                    await CheckImportTrackedAsync(task, token);
                    return false;
                }
                else
                {
                    var summary = await _dhis.GetSummaryImport(response.resultTasks[0].category, response.resultTasks[0].uid, token);
                    Console.Write("\nResultado de Importación : " + summary.ToString());
                    summaryImport.Add(summary);
                    summaryImportW.Add(JsonConvert.SerializeObject(summaryImport));
                    if (!String.IsNullOrEmpty(summary)) {
                        errorSummary = await ReadErrorSummaryAsync(summaryImportW, token); 
                    }

                    if (!String.IsNullOrEmpty(statusSummary))
                    {
                       
                        if (statusSummary == "ERROR")
                        {
                            string errorSummary = response.resultTasks[0].message;
                            if (summary == "null")
                            {
                                summary = errorSummary;
                                error += "\n" + errorSummary;
                            }
                            Console.Write("\nError resultado de Importación : " + summary.ToString());
                        }
                    }
                    
                    blockSuccess = blockSuccess + 1;
                    Console.Write("\nFin CheckImportTrackedAsync ");
                    return true;
                }

            }
            catch (Exception e)
            {
                error += "\n" + e.Message;
                Console.Write("\nError de resultado de Importación : " + error.ToString());
                return false;
            }
        }


        /// <summary>
        /// Lee archivo(s) de extensión .CSV, con su respectivo separador (coma ó punto y coma), 
        /// para generar los objetos iterables RowFile y/o RowFileLab (casos y/o laboratorio - primer y/o segundo archivo)
        /// </summary>
        /// <param name="command"> Clase que contiene todos los atributos relacionados a la importación (1 y 2do arhivo, usuario, fecha inicial y final, etc)</param>
        /// <returns>Retorna el objeto iterable del archivo obligatorio, una lista de arrays de listas</returns>
        public List<ArrayList> ReadCSV(historyCreateCommand command)
        {
            try
            {
                Console.Write("\nInicio de ReadCSV");
                commandGeneral = command;

                historyCreateCommand cmd = new historyCreateCommand();
                startDate = command.startdate + "-01-01";
                endDate = (command.enddate + 1) + "-01-01";
                //Léctura de archivo # 2
                if (command.CsvFile01 != null)
                {
                    string fileExtension = Path.GetExtension(commandGeneral.CsvFile01.FileName);
                    if (fileExtension == ".csv")
                    {
                        readerLab = new StreamReader(command.CsvFile01.OpenReadStream());
                        headersLab = readerLab.ReadLine().Split(command.separator.ToString());
                        if (headersLab.Length <= 1)
                        {
                            error += "\nEl segundo archivo no tiene el formato solicitado, separación por (,) ó (;)";
                            Console.Write("\nError de ReadCSV" + error.ToString());
                        }
                        headersLab = headersLab.Select(s => s.ToUpperInvariant()).ToArray();
                        string lineLab;
                        ArrayList listLab;

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
                    else
                    {
                        error += "\nEl segundo archivo " + Path.GetExtension(commandGeneral.CsvFile01.FileName) + " " + commandGeneral.CsvFile01.FileName + "  no es compatible con los archivos aceptados (*.csv (separado por , ó ;), *.xls y *.xlsx)";
                        Console.Write("\nError de ReadCSV" + error.ToString());
                    }
                }
                //Léctura de archivo # 1
                string line;
                ArrayList list;
                reader = new StreamReader(command.CsvFile.OpenReadStream());

                headers = reader.ReadLine().Split(command.separator.ToString());
                headers = headers.Select(s => s.ToUpperInvariant()).ToArray();
                if (headers.Length <= 1)
                {
                    error += "\nEl primer archivo no tiene el formato solicitado, separación por (,) ó (;)";
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
                error += "\n" + e.Message;
                Console.Write("\nError de ReadCSV : " + error.ToString());
            }
            return RowFile;
        }


        /// <summary>
        /// Lee archivo(s) de extensión .XLSX, con su respectivo separador (coma ó punto y coma), 
        /// para generar los objetos iterables RowFile y/o RowFileLab (casos y/o laboratorio - primer y/o segundo archivo)
        /// </summary>
        /// <param name="command"> Clase que contiene todos los atributos relacionados a la importación (1 y 2do arhivo, usuario, fecha inicial y final, etc)</param>
        /// <returns>Retorna el objeto iterable del archivo obligatorio, una lista de arrays de listas</returns>
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
                //Léctura de hoja # 2
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

                //Léctura de hoja # 1
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
                error += "\n" + e.Message;
                Console.Write("\nError de ReadXLSX : " + error.ToString());
            }
            return RowFile;

        }

        /// <summary>
        /// Lee archivo(s) de extensión .XLS, con su respectivo separador (coma ó punto y coma), 
        /// para generar los objetos iterables RowFile y/o RowFileLab (casos y/o laboratorio - primer y/o segundo archivo)
        /// </summary>
        /// <param name="command"> Clase que contiene todos los atributos relacionados a la importación (1 y 2do arhivo, usuario, fecha inicial y final, etc)</param>
        /// <returns>Retorna el objeto iterable del archivo obligatorio, una lista de arrays de listas</returns>
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
                    //Léctura de hoja # 2
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
                //Léctura de hoja # 1
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
                error += "\n" + e.Message;
                Console.Write("\nError de ReadXLS: " + error.ToString());
            }

            return RowFile;
        }

        /// <summary>
        ///  Método principal, donde se valida la extensión del archivo, se envia según la misma a los diferentes métodos para llegar a el ó los objetos iterables
        /// </summary>
        /// <param name="command">Clase que contiene todos los atributos relacionados a la importación (1 y 2do arhivo, usuario, fecha inicial y final, etc)</param>
        /// <param name="cancellation"></param>
        /// <returns>No retorna nada</returns>
        public async Task Handle(historyCreateCommand command, CancellationToken cancellation)
        {
            try
            {
                commandGeneral = command;
                command.reponse = response;
                userSetting = await loginQueyService.GetUserSetting(commandGeneral.token);
                ExternalImportDataApp = await _dhis.GetAllProgramAsync(commandGeneral.token);
                objprogram = ExternalImportDataApp.Programs.Where(a => a.Programid.Equals(commandGeneral.Programsid)).FirstOrDefault();
                Organisation = await _dhis.GetAllOrganisation(commandGeneral.token); //crear el objeto de tipo AddEventDto
                var contentOrg = JsonConvert.SerializeObject(Organisation);
                var _set = _importSettings;
                uploadBlock = _set.Services[0].Block && !_set.Services[0].Individual;
                nameFile = command.CsvFile.FileName;
                if (command.CsvFile01 != null)
                {
                    nameFileLab = commandGeneral.CsvFile01.FileName;
                }

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
                if (fileExtension != ".csv")
                {
                    if (fileExtension != ".xlsx")
                    {
                        if (fileExtension != ".xls")
                        {
                            error += "\nEl tipo de archivo " + Path.GetExtension(commandGeneral.CsvFile.FileName) + " " + commandGeneral.CsvFile.FileName + "  no es compatible con los archivos aceptados (*.csv (separado por , ó ;), *.xls y *.xlsx)";

                        }
                    }
                }

                //unidades organizativas              
                OrganisationUnit ounitsFirst = new OrganisationUnit();
                int colounitsFirst = Array.IndexOf(headers, objprogram.Orgunitcolumm.ToUpperInvariant());
                if (colounitsFirst == -1)
                {
                    error += "\nEl archivo no tiene la estructura correcta, posiblemente no tiene la Unidad Organizativa";
                }
                var Firtsline = RowFile[0];
                ounitvalueFirst = Firtsline[colounitsFirst].ToString();

                ouFirts = Organisation.OrganisationUnits.Find(x => x.code == Firtsline[colounitsFirst].ToString());

                if (ouFirts is not null)
                {
                    oupath = ouFirts.path.Split("/")[2];
                    var oupathFull = await _dhis.GetOrganisationUnit(commandGeneral.token, oupath); //crear el objeto de tipo AddEventDto
                    country = oupathFull.name;
                }
                else
                {
                    error += "\nNo existe la Unidad Organizativa: " + Firtsline[colounitsFirst].ToString();
                    state = 5;
                }

                backgroundTask.StartAsync(OrchestratorAsync(oupath, startDate, endDate, objprogram.Programid, command.token, RowFile));
            }
            catch (Exception e)
            {
                error += "\n" + e.Message;
                Console.Write("\nError Handle : " + error.ToString());
                command.reponse = error;
                EmailErrorImport();
                state = 6;
            }
        }

        /// <summary>
        ///Guarda el summary (el resultado de la importación), con otros campos adicionales (programa, usuario, fecha y hora, pais etc)en la base de datos
        /// </summary>
        /// <param name="userLogin">Usuario con el esta logueado</param>
        /// <param name="program">programa</param>
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
                error += "\n" + e.Message;
                Console.Write("\nError Guardado Summry DB : " + error.ToString());
            }
        }


        /// <summary>
        /// Metódo para envio de email con errores, no se importo la data
        /// </summary>
        public void EmailErrorImport()
        {
            string subject = _importSettings.Services[0].SubjectError;
            string body = _importSettings.Services[0].BodyError;
            // error = !String.IsNullOrEmpty(error) ? _importSettings.Services[0].TitleError + "\n" + error : "";

            sendMailObj.SenEmailImport(_importSettings.Services[0].Server, subject, body + error, userSetting.email, _importSettings.Services[0].EmailFrom, _importSettings.Services[0].Pass, _importSettings.Services[0].Port, "El ó los archivo(s): " + nameFile + " " + nameFileLab);
        }

        /// <summary>
        /// Método propio de la clase para actualización
        /// </summary>
        /// <param name="command">Clase que contiene todos los atributos relacionados a la importación (1 y 2do arhivo, usuario, fecha inicial y final, etc)</param>
        /// <param name="cancellation"></param>
        /// <returns>No retorna nada</returns>
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



        /// <summary>
        /// Léctura del Summary y extracción de los errores para adjuntarlos al email
        /// </summary>
        /// <param name="result">Lista de resultados (Summary)</param>
        /// <param name="token">Token de autenticación</param>
        /// <returns></returns>
        public async Task<string> ReadErrorSummaryAsync(List<string> result, string token)
        {
            string ResponseError = "";
            if (result.Count > 0)
            {
                int idresult = 0;
                for (int i = 0; i < result.Count(); i++)
                {
                    var json = result[i];

                    var jsonpars = json.Remove(0, 1);
                    int lg = json.Length - 2;
                    jsonpars = jsonpars.Remove(lg, 1);
                    jsonpars = jsonpars.Replace("\\", "");
                    jsonpars = "[" + jsonpars + "]";
                    jsonpars = jsonpars.Replace("[\"", "[");
                    jsonpars = jsonpars.Replace("\"]", "]");
                    jsonpars = jsonpars.Replace("}\",\"{", "},{");
                    try
                    {

                        var dhisResponse = JsonConvert.DeserializeObject<List<Root>>(jsonpars);
                        foreach (Root item in dhisResponse)
                        {
                            statusSummary = item.status;
                            foreach (ImportSummaryDhis itemsum in item.importSummaries)
                            {

                                if (itemsum.conflicts.Count > 0)
                                {
                                    var st = itemsum.conflicts[0];
                                    foreach (ConflictDhis conflict in itemsum.conflicts)
                                    {
                                        try
                                        {
                                            var Case_ID = await _dhis.GetTrackedreferenceAsync(token, itemsum.reference);
                                            ResponseError = "\nCase_Id " + Case_ID.attributes[0].value + " : " + conflict.value + ";" + ResponseError;
                                        }
                                        catch (Exception e)
                                        {
                                            ResponseError = conflict.value + ";" + ResponseError;
                                        }
                                            

                                    }
                                }
                                foreach (ImportSummaryDhis itemed in itemsum.enrollments.importSummaries)
                                    foreach (ImportSummaryDhis itemev in itemed.events.importSummaries)
                                    {
                                        if (itemev.conflicts.Count > 0)
                                        {
                                            foreach (ConflictDhis conflict in itemev.conflicts)
                                            {
                                                foreach (ProgramStage ps in objprogram.programStages)
                                                    foreach (ProgramStageDataElement dtele in ps.programStageDataElements)
                                                        if (dtele.dataElement.id == conflict.@object)
                                                        {
                                                            var Case_ID = await _dhis.GetTrackedreferenceAsync(token, itemsum.reference);
                                                            ResponseError = "Case_Id " + Case_ID.attributes[0].value + " : " + dtele.dataElement.name + "," + conflict.value + ";" + ResponseError;
                                                        }
                                            }


                                        }
                                    }


                            }
                        }
                    }
                    catch (Exception e)
                    {

                        ResponseError = "";
                    }


                    idresult++;
                }
            }
            return ResponseError;
        }
    }
}