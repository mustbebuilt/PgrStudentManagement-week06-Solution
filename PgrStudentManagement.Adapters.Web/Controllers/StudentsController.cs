using Microsoft.AspNetCore.Mvc;
using PgrStudentManagement.Adapters.Web.Mappers;
using PgrStudentManagement.Business.Students.ViewStudentEnrolments;

namespace PgrStudentManagement.Adapters.Web.Controllers;

public class StudentsController : Controller
{
    private readonly IViewStudentEnrolmentsUseCase _useCase;
    private readonly StudentEnrolmentsWebMapper _mapper;

    public StudentsController(
        IViewStudentEnrolmentsUseCase useCase,
        StudentEnrolmentsWebMapper mapper)
    {
        _useCase = useCase;
        _mapper = mapper;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<IActionResult> Enrolments(
        string studentNumber,
        CancellationToken cancellationToken)
    {
        var input = new ViewStudentEnrolmentsInput(studentNumber ?? string.Empty);
        var output = await _useCase.ExecuteAsync(input, cancellationToken);
        var viewModel = _mapper.ToViewModel(output);
        return View(viewModel);
    }
}
