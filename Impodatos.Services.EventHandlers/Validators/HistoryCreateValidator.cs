using FluentValidation;
using Impodatos.Services.EventHandlers.Commands;

namespace Impodatos.Services.EventHandlers.Validators
{
    public class historyCreateValidator : AbstractValidator<historyCreateCommand>
    {
      public historyCreateValidator()
        {
            RuleFor(x => x.UserLogin).NotNull().WithMessage("El campo usuario esta vacio");
            RuleFor(x => x.Programsid).NotNull().WithMessage("El campo Id programa esta vacio");
            //RuleFor(x => x.IdprogramStages).NotNull().WithMessage("El campo ProgramStage esta vacio");
            RuleFor(x => x.CsvFile).NotNull().WithMessage("No ha subido un archivo CSV");
            //RuleFor(x => x.ExcelFile.ContentType).Equal("application/vnd.ms-excel").WithMessage("El archivo adjunto debe ser en formato .CSV");
        }
    }
}
