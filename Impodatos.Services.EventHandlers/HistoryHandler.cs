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
using Z.Expressions;
namespace Impodatos.Services.EventHandlers
{
    public class historyHandler :
        INotificationHandler<historyCreateCommand>,
        INotificationHandler<historyUpdateCommand>
    {
        private readonly ApplicationDbContext _context;
        private readonly IDhisQueryService _dhis;

        public historyHandler(ApplicationDbContext context, IDhisQueryService dhis)
        {
            _context = context;
            _dhis = dhis;
        }

        public async Task Handle(historyCreateCommand command, CancellationToken cancellation)
        {
            int cont = 0;
            int contbad = 0;
            int total = 0;
            int contupload = 0;
            int contdelete = 0;
            List<string> jsonResponse = new List<string>();
            Program objprogram = new Program();
            //leemos el archivo y guardamos en memoria
            //if
            historyCreateCommand cmd = new historyCreateCommand();
            cmd.CsvFile = command.CsvFile;
            var readerDate = new StreamReader(cmd.CsvFile.OpenReadStream());
            var lstDate = new List<Int32>();
            var propiedadesDate = readerDate.ReadLine().Split(';');
            string startDate = command.startdate + "-01-01";
            string endDate = (command.enddate + 1) + "-01-01";
            //while (!readerDate.EndOfStream)
            //{
            //    var valoresDate = readerDate.ReadLine().Split(';');
            //    int colDate = Array.IndexOf(propiedadesDate, "dtReportedLocal");
            //    if (valoresDate[colDate] != null && valoresDate[colDate].Trim().Length == 10)
            //        lstDate.Add(Convert.ToDateTime(valoresDate[colDate]).Year);
            //}
            //if (lstDate.Count > 0)
            //{
            //    lstDate.Sort();
            //    startDate = lstDate[0].ToString() + "-01-01";
            //    endDate = (Convert.ToInt32(lstDate[lstDate.Count - 1]) + 1).ToString() + "-01-01";
            //}

            var reader = new StreamReader(command.CsvFile.OpenReadStream());
            //var reader01 = new StreamReader(command.CsvFile01.OpenReadStream());

            //con el metodo ReadLine leemos la primera linea y guardamos el array en la variable
            var propiedades = reader.ReadLine().Split(';');
            propiedades = propiedades.Select(s => s.ToUpperInvariant()).ToArray();
            //var propiedades01 = reader01.ReadLine().Split(';');

            //recorremos archivo hasta el final

            try
            {

                int codes = 0;
                //var TrackeduidGeneratedDto = await _dhis.GetUidGenerated("10000", command.token);
                var ExternalImportDataApp = await _dhis.GetAllProgramAsync(command.token);
                objprogram = ExternalImportDataApp.Programs.Where(a => a.Programid.Equals(command.Programsid)).FirstOrDefault();
                OrganisationUnitsDto Organisation = new OrganisationUnitsDto();
                Organisation = await _dhis.GetAllOrganisation(command.token); //crear el objeto de tipo AddEventDto
                var contentOrg = JsonConvert.SerializeObject(Organisation);
                string oupath = null;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
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
                    if (dty == command.startdate || dty == command.enddate)
                    {
                        List<TrackedEntityInstances> listtrackedInstDto = new List<TrackedEntityInstances>();
                        List<Enrollment> listEnrollment = new List<Enrollment>();
                        AddTrackedDto trackedDto = new AddTrackedDto();
                        AddEnrollmentDto enrollment = new AddEnrollmentDto();
                        string trackedEntityInstance = "";
                        //if(codes == 9998) { TrackeduidGeneratedDto = await _dhis.GetUidGenerated("10000", command.token); codes = 0; }
                        //Genero un uid 
                        var TrackeduidGeneratedDto = await _dhis.GetUidGenerated("1", command.token);
                        //leemos cada una de las lineas a partir de la linea dos con el metodo ReadLine, el cual va iterando cada linea

                        cont = cont + 1;
                        //cargar las unidades organizativas en un array para recorrerlo localmente y friltrar para crear el tracked
                        TrackedEntityInstances trackedInstDto = new TrackedEntityInstances();

                        OrganisationUnit ounits = new OrganisationUnit();
                        int colounits = Array.IndexOf(propiedades, objprogram.Orgunitcolumm.ToUpperInvariant());
                        string ounitvalue = valores[colounits];
                        bool okou = false;
                        foreach (OrganisationUnit ou in Organisation.OrganisationUnits)
                        {
                            if (ou.code == ounitvalue)
                            {
                                ounits = ou;
                                okou = true;
                                if (oupath == null)
                                {
                                    oupath = ou.path.Split("/")[2];
                                    //var statusclear =  clearEvents(oupath, startDate, endDate, command.token);
                                    var setClean = await _dhis.SetCleanEvent(oupath, startDate, endDate, command.token);
                                    if (setClean.events.Count > 0)
                                    {
                                        var dropEvens = await _dhis.AddEventClear(setClean, command.token, "?strategy=DELETE&includeDeleted=true&async=true");
                                        contdelete = Convert.ToInt32(dropEvens.response.total);
                                        _dhis.SetMaintenanceAsync(command.token);
                                    }
                                }
                            }
                            if (okou == true) break;
                        }
                        int caseid = Array.IndexOf(propiedades, "CASE_ID");
                        string caseidvalue = valores[caseid];
                        //Validamos si el tracked ya existe:  verificar por que se estan creando repetidos
                        string ouid = ounits.id;
                        var validatetraked = await _dhis.GetTracket(caseidvalue, ouid, command.token);
                        if (validatetraked.trackedEntityInstances.Count > 0)
                            trackedInstDto.trackedEntityInstance = validatetraked.trackedEntityInstances[0].trackedEntityInstance;
                        else
                            trackedInstDto.trackedEntityInstance = TrackeduidGeneratedDto.Codes[0].ToString();

                        trackedInstDto.trackedEntityType = objprogram.Trackedentitytype;
                        int ideventdate = Array.IndexOf(propiedades, objprogram.Incidentdatecolumm.ToUpperInvariant());
                        string eventdate = valores[ideventdate];
                        trackedInstDto.orgUnit = ounits.id;

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
                        var SequentialDto = await _dhis.GetSequential("1", command.token);
                        Attribut attributSq = new Attribut();
                        attributSq.attribute = "mxKJ869xJOd";
                        attributSq.value = SequentialDto[0].value;
                        listAttribut.Add(attributSq);
                        trackedInstDto.attributes = listAttribut;
                        listtrackedInstDto.Add(trackedInstDto);
                        trackedDto.trackedEntityInstances = listtrackedInstDto;
                        AddTracketResultDto trakedResultDto = new AddTracketResultDto();
                        AddEnrollmentResultDto enrollResultDto = new AddEnrollmentResultDto();
                        try
                        {
                            trakedResultDto = await _dhis.AddTracked(trackedDto, command.token); //crear el objeto de tipo AddTrackedDto
                        }
                        catch (Exception e) { }
                        if (trakedResultDto.Status == "OK")
                        {
                            //validamos el enrollment
                            var validateenrollment = await _dhis.GetEnrollment(trackedDto.trackedEntityInstances[0].trackedEntityInstance, ounits.id, command.token);
                            if (validateenrollment.enrollments.Count == 0)
                            {
                                Enrollment enrollmentDto = new Enrollment();
                                //enrollment
                                enrollmentDto.trackedEntityInstance = trackedInstDto.trackedEntityInstance;
                                enrollmentDto.program = objprogram.Programid;
                                enrollmentDto.status = "ACTIVE";
                                enrollmentDto.orgUnit = trackedInstDto.orgUnit;
                                enrollmentDto.enrollmentDate = enrollmentDatecolumm;
                                enrollmentDto.incidentDate = incidentDatecolumm;
                                listEnrollment.Add(enrollmentDto);
                                enrollment.enrollments = listEnrollment;
                                enrollResultDto = await _dhis.AddEnrollment(enrollment, command.token);
                            }
                            else
                            {
                                enrollResultDto.Status = validateenrollment.enrollments[0].status;
                            }

                        }
                        //Event
                        AddEventsDto eventDto = new AddEventsDto();
                        if (trakedResultDto.Status == "OK" && enrollResultDto.Status == "ACTIVE")
                        {
                            List<ProgramStageDataElement> dteObjarray = new List<ProgramStageDataElement>();

                            List<AddEventDto> listEvent = new List<AddEventDto>();
                            foreach (ProgramStage ps in objprogram.programStages)
                            {
                                List<DataValue> listDataValue = new List<DataValue>();
                                #region lab
                                //if (ps.id.Equals("sNQqHHN5gi3") && reader01 != null)
                                //{
                                //    try
                                //    {
                                //        while (!reader01.EndOfStream)
                                //        {
                                //            List<DataValue> listDataValue = new List<DataValue>();
                                //            var valoreslab = reader01.ReadLine().Split(';');
                                //            if (valoreslab[0].Equals(caseidvalue))
                                //            {

                                //                foreach (ProgramStageDataElement dtelab in ps.programStageDataElements)
                                //                {
                                //                    try
                                //                    {
                                //                        DataValue datavalue = new DataValue();
                                //                        int idval = Array.IndexOf(propiedades01, dtelab.dataElement.column);
                                //                        if (idval >= 0)
                                //                        {
                                //                            datavalue.dataElement = dtelab.dataElement.id;
                                //                            datavalue.value = valoreslab[idval];
                                //                            listDataValue.Add(datavalue);
                                //                        }

                                //                    }
                                //                    catch (Exception e) { }
                                //                }
                                //                if (listDataValue.Count > 0)
                                //                {
                                //                    string tkins = "";
                                //                    if (trackedEntityInstance.Trim().Length == 0)
                                //                        tkins = trackedInstDto.trackedEntityInstance;
                                //                    else
                                //                        tkins = trackedEntityInstance;
                                //                    var storedBy = command.UserLogin;
                                //                    UidGeneratedDto EventuidGeneratedDto = new UidGeneratedDto();
                                //                    EventuidGeneratedDto = await _dhis.GetUidGenerated("1", command.token);
                                //                    string code = "";
                                //                    code = EventuidGeneratedDto.Codes[0].ToString();
                                //                    AddEventDto objEventDto = new AddEventDto
                                //                    {
                                //                        programStage = ps.id,
                                //                        program = objprogram.Programid,
                                //                        orgUnit = trackedInstDto.orgUnit,
                                //                        eventDate = eventdate,
                                //                        status = "ACTIVE",                                              
                                //                        storedBy = storedBy,
                                //                        trackedEntityInstance = tkins,
                                //                        event_ = code,
                                //                        dataValues = listDataValue
                                //                     };

                                //                    listEvent.Add(objEventDto);

                                //                }
                                //            }

                                //        }
                                //    }
                                //    catch (Exception e) { }

                                //}
                                //else
                                //{
                                #endregion
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

                                        var storedBy = command.UserLogin;
                                        UidGeneratedDto EventuidGeneratedDto = new UidGeneratedDto();
                                        EventuidGeneratedDto = await _dhis.GetUidGenerated("1", command.token);
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

                        }
                        try
                        {
                            var eventsResultDto = await _dhis.AddEvent(eventDto, command.token);
                            string rrr = Convert.ToString(JsonConvert.SerializeObject(eventsResultDto));
                            Console.Write("AddEvent: " + eventsResultDto.Status);
                            eventDto = new AddEventsDto();
                            contupload = contupload + 1;
                            total = cont;
                        }
                        catch (Exception e)
                        {

                            int bad = contbad;
                            var errorevent = new
                            {

                            };

                            //string jsonDto = JsonConvert.SerializeObject(eventDto);
                            string val = Convert.ToString(JsonConvert.SerializeObject(valores));
                            jsonResponse.Add(val); //+ jsonDto);
                        }
                    }
                }
                total = cont;
            }
            catch (Exception e) { Console.WriteLine("{0} Exception caught.", e); }
            //convertimos el archivo a un array de Bytes

            //AddEventDto objEventDto = new AddEventDto
            //{
            //    programStage = "1",
            //    program = "2",
            //    orgUnit = "3",
            //    eventDate = "2022-06-16",
            //    status = "ACTIVE",
            //    storedBy = "lcobo",
            //    trackedEntityInstance = "4",
            //    event_ = "5"
            //};
            //string jsonDto = JsonConvert.SerializeObject(objEventDto);
            //jsonResponse.Add(jsonDto); //+ jsonDto)
            byte[] data = null;
            var fileByte = new BinaryReader(command.CsvFile.OpenReadStream());
            int i = (int)command.CsvFile.Length;
            data = fileByte.ReadBytes(i);
            //agregamos al contexto la informacion aguardar


            await _context.AddAsync(new history
            {
                programsid = objprogram.Programname,
                uploads = contupload,
                deleted = contdelete,
                jsonset = JsonConvert.SerializeObject(jsonResponse),
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
