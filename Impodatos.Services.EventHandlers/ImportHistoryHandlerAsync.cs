using Impodatos.Domain;
using Impodatos.Persistence.Database;
using Impodatos.Services.EventHandlers.Commands;
using Impodatos.Services.Queries;
using Impodatos.Services.Queries.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;


namespace Impodatos.Services.EventHandlers
{
   public class ImportHistoryHandlerAsync :
     INotificationHandler<historyCreateCommand>,
        INotificationHandler<historyUpdateCommand>
    {
        private readonly ApplicationDbContext _context;
        private readonly IDhisQueryService _dhis;
        public int state = 1;
        public string oupath;
        public string startDate;
        public string endDate;
        public string program;
        public string token;
        public string dataIterable;
        public int cont = 0;
        public int contupload = 0;
        public int contBlock = 0;
        public bool uploadBlock;
        public historyCreateCommand commandGeneral;
        public int contFiles = 0;
        public int blockSuccess = 0;
        public List<string> summaryImport = new List<string>();
        public bool endWhile = false; 

        public string[] propiedades;
        public Program objprogram = new Program();
        public OrganisationUnit ouFirts = new OrganisationUnit();
        public List<string> jsonResponse = new List<string>();

        public StreamReader reader;

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
                        Block = Convert.ToBoolean(s.GetSection("block").Value)
                    }).ToList()
                    
                };
            }
        }

        public async Task OrchestratorAsync(string oupath, string startDate, string endDate, string program, string token, string dataIterable) {
          var TaskResult = "";
          int step = 1;

            do {
                switch (state) {
                    case 1:
                        step = 1;
                        TaskResult = await CleanEventsAsync(oupath,program, startDate, endDate, token);
                        break;
                    case 2:
                        step = 2;
                        await CleanEnrollmentsAsync(program, oupath,  startDate,  endDate, token);
                        break;
                    case 3:
                        step = 3;
                        //decimal cantidadFilas = Math.Round(Convert.ToDecimal(reader.ReadToEnd().Split(';').Length) / propiedades.Count());
                        await ImportDataAsync();
                        if (endWhile ) {

                           if ((contBlock  == blockSuccess) || (contBlock +1 == blockSuccess))
                            {
                                state = 4;
                            }                            
                          } 
                        break;                
                }
                if (TaskResult != "")
                {
                    await CheckImportStateAsync(state, step, TaskResult, token, 1);                    
                } 
            }
            while (state < 4);
        }      

        public async Task<bool> CheckImportStateAsync(int state ,int step, string task, string token, int numImport = 1)
        {
            var response = await _dhis.GetStateTask(task.Replace("/api", ""), token);
            var completed = response.resultTasks[0].completed;
            if (!completed)
            {               
                await CheckImportStateAsync(state,step, task, token, numImport);
                return false;
            }
            else
            {
                switch (step) { 
                    case 1:
                        state = 2;
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
            //await  CheckImportStateAsync(caseNum, task, token, numImport);
            //var tasktimer = Task.Run(async () => await CheckImportStateAsync(caseNum, task, token, numImport));
            //tasktimer.Wait(TimeSpan.FromSeconds(1000));
            //}            
        }       

        public async Task<string> CleanEventsAsync(string oupath, string program ,string startDate, string endDate, string token)
        {
            int contdelete = 0;
            var setClean = await _dhis.SetCleanEvent(oupath, program, startDate, endDate, token);
            if (setClean.events.Count > 0)
            {
                var dropEvens = await _dhis.AddEventClear(setClean, token, "?strategy=DELETE&includeDeleted=true&async=true");
                contdelete = Convert.ToInt32(dropEvens.response.total);
                _dhis.SetMaintenanceAsync(token);
                return dropEvens.response.relativeNotifierEndpoint.Replace("/api", "");
            }
            else {
                state = 2;
            }
            return "";
        }


        public async Task CleanEnrollmentsAsync(string program, string oupath,  string startDate, string endDate, string token)
        {
            int contdelete = 0;
            //var setCleanEnrolloment = await _dhis.GetEnrollment(program, oupath,  token);
            var setCleanEnrolloment = await _dhis.SetCleanEnrollment(oupath, program, startDate,  endDate, token);
            if (setCleanEnrolloment.enrollments.Count > 0)
            {
                var dropEnrrollments = await _dhis.AddEnrollmentClear(setCleanEnrolloment, token, "?strategy=DELETE");
                contdelete = Convert.ToInt32(dropEnrrollments.response.total);
                _dhis.SetMaintenanceAsync(token);
                if (contdelete > 0) {
                    state = 3;
                }
            }
            else {
                state = 3;
            }
           
        }

        public async Task ImportDataAsync()
        {
            var _set = _importSettings;
            int cont = 0;
            int contbad = 0;
            int total = 0;
            //int contupload = 0;

            int SizeUpload = _set.Services[0].SizeUpload;

            AddTrackedDto trackedDto = new AddTrackedDto();
            AddTrackedDto trackedIndDto = new AddTrackedDto();

            List<TrackedEntityInstances> listtrackedInstDtoFull = new List<TrackedEntityInstances>();
            List<Enrollment> listEnrollmentFull = new List<Enrollment>();

            AddEnrollmentDto enrollments = new AddEnrollmentDto();
            AddEventsDto eventDto = new AddEventsDto();
            string line;

            reader = new StreamReader(commandGeneral.CsvFile.OpenReadStream());
            propiedades = reader.ReadLine().Split(';');
            propiedades = propiedades.Select(s => s.ToUpperInvariant()).ToArray();
            while ((line = reader.ReadLine()) != null)
            {
                contFiles = contFiles + 1;
                var valores = line.Split(';');
                int cic = 0;
                Console.Write("Ciclos: " + cic.ToString());
                cic++;
                int dtRashOn = Array.IndexOf(propiedades, "DTRASHONSET"); //error
                string dtRashOnval = valores[dtRashOn];
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

                    int caseid = Array.IndexOf(propiedades, "CASE_ID");
                    string caseidvalue = valores[caseid];
                    //Validamos si el tracked ya existe:  verificar por que se estan creando repetidos
                    var validatetraked = await _dhis.GetTracket(caseidvalue, ouFirts.id, commandGeneral.token);
                    if (validatetraked.trackedEntityInstances.Count > 0)
                        trackedInstDto.trackedEntityInstance = validatetraked.trackedEntityInstances[0].trackedEntityInstance;
                    else
                        trackedInstDto.trackedEntityInstance = TrackeduidGeneratedDto.Codes[0].ToString();

                    trackedInstDto.trackedEntityType = objprogram.Trackedentitytype;
                    int ideventdate = Array.IndexOf(propiedades, objprogram.Incidentdatecolumm.ToUpperInvariant());
                    string eventdate = valores[ideventdate];
                    trackedInstDto.orgUnit = ouFirts.id;

                    List<Attribut> listAttribut = new List<Attribut>();
                    string enrollmentDatecolumm = "";
                    string incidentDatecolumm = "";
                    var id = Array.IndexOf(propiedades, objprogram.Enrollmentdatecolumm.ToUpperInvariant());
                    enrollmentDatecolumm = valores[id];
                    //enrollmentDatecolumm = valores[id].Split('/')[2].PadLeft(2, '0') + "-" + valores[id].Split('/')[1] + "-" + valores[id].Split('/')[0].PadLeft(2, '0');
                    var idi = Array.IndexOf(propiedades, objprogram.Incidentdatecolumm.ToUpperInvariant());
                    incidentDatecolumm = valores[idi];
                    //incidentDatecolumm = valores[idi].Split('/')[2].PadLeft(2, '0') + "-" + valores[idi].Split('/')[1] + "-" + valores[idi].Split('/')[0].PadLeft(2, '0');

                    foreach (Queries.DTOs.Attribute at in objprogram.Attribute)
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
                                    if (at.Id.Equals(objprogram.caseNum) && listtrackedInstDto.Count > 0)
                                    {
                                        foreach (TrackedEntityInstances tki in listtrackedInstDto)
                                            foreach (Attribut att in tki.attributes)
                                                if (att.value.Equals(valores[idval]))
                                                    trackedEntityInstance = tki.trackedEntityInstance;
                                    }

                                    if (at.Name.Equals("Date of birth") || at.Name.Equals("Date of rash onset") || at.Name.Equals("WEA - Clasificación final"))
                                        attribut.value = valores[idval];// valores[idval].Split('/')[2].PadLeft(2, '0') + "-" + valores[idval].Split('/')[1] + "-" + valores[idval].Split('/')[0].PadLeft(2, '0');
                                    else
                                        attribut.value = valores[idval];
                                    if (at.Name.Equals("Is date of birth known"))
                                        attribut.value = valores[idval] == null ? "false" : "true";
                                    if (attribut != null)
                                    {
                                        listAttribut.Add(attribut);
                                    }
                                }
                            }
                            catch (Exception e) { }
                        }
                    }
                    var SequentialDto = await _dhis.GetSequential("1", commandGeneral.token);
                    trackedInstDto.attributes = listAttribut;
                    listtrackedInstDto.Add(trackedInstDto);
                    listtrackedInstDtoFull.Add(trackedInstDto);
                    trackedDto.trackedEntityInstances = listtrackedInstDto;
                    trackedIndDto.trackedEntityInstances = listtrackedInstDtoFull;

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

                    List<ProgramStageDataElement> dteObjarray = new List<ProgramStageDataElement>();

                    List<AddEventDto> listEvent = new List<AddEventDto>();
                    foreach (ProgramStage ps in objprogram.programStages)
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
                                        datavalue.dataElement = dte.dataElement.id;
                                        datavalue.value = valores[idval];
                                        //if(datavalue.dataElement == "w2GWdKFVkVk")  Validar que el formato de esta fecha sea yyyy-mm-dd
                                        listDataValue.Add(datavalue);
                                        cont = cont + 1;
                                    }
                                }
                                catch (Exception e) { contbad = contbad + 1; }

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
                                catch (Exception e) { }

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
                        catch (Exception e) { }
                        //}
                    }
                    eventDto.events = listEvent;
                    // Importante para envio en bloques de 100 o como se ajuste la configuracion
                    if (uploadBlock)
                    {
                        enrollmentFullDto.Eev = listEvent;
                        trackedInstDto.Eenr = listEnrollment;
                        var preuba = JsonConvert.SerializeObject(trackedIndDto).Replace("Eenr", "enrollments").Replace("Eev", "events");
                    }                           

                    try
                    {
                        contupload = contupload + 1;
                        total = cont;
                    }
                    catch (Exception e)
                    {
                        int bad = contbad;
                        var errorevent = new
                        { };

                        string val = Convert.ToString(JsonConvert.SerializeObject(valores));
                        jsonResponse.Add(val); //+ jsonDto);
                    }
                }

                if (trackedIndDto.trackedEntityInstances.Count >= SizeUpload)
                {
                    AddTracketResultDto trakedResultDto = new AddTracketResultDto();
                    try
                    {
                        contBlock = contBlock + 1;
                        var resultDto = await _dhis.AddTracked(trackedIndDto, commandGeneral.token);
                        var res = await CheckImportTrackedAsync(resultDto.Response.relativeNotifierEndpoint, commandGeneral.token);
                    }
                    catch (Exception e) { }

                    if (!uploadBlock)
                    {
                        AddEnrollmentResultDto enrollResultDto = new AddEnrollmentResultDto();
                        if (trakedResultDto.Status == "OK")
                        {
                            try
                            {
                                enrollResultDto = await _dhis.AddEnrollment(enrollments, commandGeneral.token);
                            }
                            catch (Exception e) { }

                        }
                        if (trakedResultDto.Status == "OK" && enrollResultDto.Status == "ACTIVE")
                        {
                            try
                            {
                                var eventsResultDto = await _dhis.AddEvent(eventDto, commandGeneral.token);

                            }
                            catch (Exception e) { }
                        }
                    }
                    trackedIndDto.trackedEntityInstances.Clear();
                }

            }//cierre de while
            total = cont;
            endWhile = true;
            var numSaved = contFiles%SizeUpload;
            // Temporal para los archivos restantes de los bloques
            if (trackedIndDto.trackedEntityInstances.Count > 0)
            {
                AddTracketResultDto trakedResultDto = new AddTracketResultDto();

                try
                {
                    var resultDto = await _dhis.AddTracked(trackedIndDto, commandGeneral.token);
                    var res = await CheckImportTrackedAsync(resultDto.Response.relativeNotifierEndpoint, commandGeneral.token);
                   
                }
                catch (Exception e) { }
            }


        }

        public async Task<bool> CheckImportTrackedAsync(string task,string token)
        {
            var response = await _dhis.GetStateTask(task.Replace("/api", ""), token);
            
            var completed = response.resultTasks[0].completed;
            if (!completed)
            {
                await CheckImportTrackedAsync(task, token);
                return false;
            }
            else
            {
                var summary = await _dhis.GetSummaryImport(response.resultTasks[0].category, response.resultTasks[0].uid, token);
                summaryImport.Add(summary);
                blockSuccess = blockSuccess + 1;
                return true;                
            }
        }

            public bool ReadData(historyCreateCommand command) {

            var _set = _importSettings;
            uploadBlock = _set.Services[0].Block && !_set.Services[0].Individual;
            
            //Program objprogram = new Program();
            //leemos el archivo y guardamos en memoria
            //if
            historyCreateCommand cmd = new historyCreateCommand();
            cmd.CsvFile = command.CsvFile;
            var readerDate = new StreamReader(cmd.CsvFile.OpenReadStream());

            var lstDate = new List<Int32>();
            var propiedadesDate = readerDate.ReadLine().Split(';');
             startDate = command.startdate + "-01-01";
             endDate = (command.enddate + 1) + "-01-01";

            reader = new StreamReader(command.CsvFile.OpenReadStream());

            propiedades = reader.ReadLine().Split(';');
            propiedades = propiedades.Select(s => s.ToUpperInvariant()).ToArray();

            if (reader.Read() > 0)
            {
                return true;
            }
            else {
                return false;
            }            
        }


        public async Task<OrganisationUnit> getOrganisationUnitAsync() {

            //con el metodo ReadLine leemos la primera linea y guardamos el array en la variable
            string ounitvalueFirst;
            //propiedades = reader.ReadLine().Split(';');
            //propiedades = propiedades.Select(s => s.ToUpperInvariant()).ToArray();

            var ExternalImportDataApp = await _dhis.GetAllProgramAsync(commandGeneral.token);
            objprogram = ExternalImportDataApp.Programs.Where(a => a.Programid.Equals(commandGeneral.Programsid)).FirstOrDefault();
            OrganisationUnitsDto Organisation = new OrganisationUnitsDto();
            Organisation = await _dhis.GetAllOrganisation(commandGeneral.token); //crear el objeto de tipo AddEventDto
            var contentOrg = JsonConvert.SerializeObject(Organisation);
            oupath = null;
            var readerTmp = reader;

            string Firtsline = readerTmp.ReadLine();

            //while (Firtsline != null) {
            var valuesFirts = Firtsline.Split(';');
            int colounitsFirst = Array.IndexOf(propiedades, objprogram.Orgunitcolumm.ToUpperInvariant());
            ounitvalueFirst = valuesFirts[colounitsFirst];

            OrganisationUnit ounitsFirst = new OrganisationUnit();
            int colounitsFirts = Array.IndexOf(propiedades, objprogram.Orgunitcolumm.ToUpperInvariant());
            string ounitvalueFirts = valuesFirts[colounitsFirts];

            ouFirts = Organisation.OrganisationUnits.Find(x => x.code == ounitvalueFirts.ToString());
            oupath = ouFirts.path.Split("/")[2];
            reader.Close();
            return ouFirts;
        }
        public async Task Handle(historyCreateCommand command, CancellationToken cancellation)
        {               

            commandGeneral = command;           
            try
            {  
                if (ReadData(command))
                {
                    await getOrganisationUnitAsync();
                    await OrchestratorAsync(oupath, startDate, endDate, objprogram.Programid, command.token, "");
                }
                
             }
            
            catch (Exception e) { Console.WriteLine("{0} Exception caught.", e); }
           
            byte[] data = null;
            var fileByte = new BinaryReader(command.CsvFile.OpenReadStream());
            int i = (int)command.CsvFile.Length;
            data = fileByte.ReadBytes(i);
            //agregamos al contexto la informacion aguardar
            await _context.AddAsync(new history
            {
                programsid = objprogram.Programname,
                uploads = contupload,
                //deleted = contdelete,
                jsonset = JsonConvert.SerializeObject(summaryImport),
                jsonresponse = "Procesado",
                state = true,
                userlogin = command.UserLogin,
                fecha = DateTime.Now,
                file = data
            });
            try
            {
                //guardamos
                await _context.SaveChangesAsync();
            }
            catch (Exception e) { }
        }       

        public async Task Handle(historyUpdateCommand command, CancellationToken cancellation)
        {
            var history = await _context.history.FindAsync(command.Id);
            history.jsonresponse = command.JsonResponse;
            history.state = command.State;

            await _context.SaveChangesAsync();
        }
    }
}
