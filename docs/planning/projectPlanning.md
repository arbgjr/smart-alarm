# Planning

Abaixo alterações necessárias com base no [projectEvolution.md](projectEvolution.md)

## 1. Domínio (Domain Layer)

### a) Novas Entidades/Value Objects

**Holiday.cs**

```csharp
public class Holiday
{
    public Guid Id { get; private set; }
    public DateTime Date { get; private set; }
    public string Description { get; private set; }

    public Holiday(DateTime date, string description)
    {
        Id = Guid.NewGuid();
        Date = date.Date;
        Description = description;
    }
}
```

**ExceptionPeriod.cs**

```csharp
public class ExceptionPeriod
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    public ExceptionPeriod(Guid userId, DateTime start, DateTime end)
    {
        if (end < start)
            throw new ArgumentException("End date must be after start date.");
        Id = Guid.NewGuid();
        UserId = userId;
        StartDate = start.Date;
        EndDate = end.Date;
    }

    public bool IsInPeriod(DateTime date) => date.Date >= StartDate && date.Date <= EndDate;
}
```

**UserHolidayPreference.cs**

```csharp
public class UserHolidayPreference
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid HolidayId { get; private set; }
    public bool DisableAlarm { get; private set; }

    public UserHolidayPreference(Guid userId, Guid holidayId, bool disableAlarm)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        HolidayId = holidayId;
        DisableAlarm = disableAlarm;
    }
}
```

### b) Ajuste em Alarm/Schedule

**Alarm.cs** (exemplo de método)

```csharp
public bool ShouldTriggerNow(DateTime now, IEnumerable<Holiday> holidays, IEnumerable<ExceptionPeriod> exceptions)
{
    if (holidays.Any(h => h.Date == now.Date))
        return false;
    if (exceptions.Any(e => e.IsInPeriod(now)))
        return false;
    // ...lógica existente...
    return true;
}
```

---

## 2. Application Layer

### a) Commands/Handlers

**ImportHolidaysCommand.cs**

```csharp
public class ImportHolidaysCommand : IRequest<Result>
{
    public IFormFile File { get; set; }
    public Guid UserId { get; set; }
}
```

**ImportHolidaysHandler.cs**

```csharp
public class ImportHolidaysHandler : IRequestHandler<ImportHolidaysCommand, Result>
{
    private readonly IHolidayRepository _holidayRepo;
    private readonly IFileParser _fileParser;

    public async Task<Result> Handle(ImportHolidaysCommand request, CancellationToken ct)
    {
        var holidays = await _fileParser.ParseHolidaysAsync(request.File);
        foreach (var holiday in holidays)
            await _holidayRepo.AddAsync(holiday, ct);
        return Result.Success();
    }
}
```

**CreateExceptionPeriodCommand.cs**

```csharp
public class CreateExceptionPeriodCommand : IRequest<Result>
{
    public Guid UserId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}
```

**CreateExceptionPeriodHandler.cs**

```csharp
public class CreateExceptionPeriodHandler : IRequestHandler<CreateExceptionPeriodCommand, Result>
{
    private readonly IExceptionPeriodRepository _repo;

    public async Task<Result> Handle(CreateExceptionPeriodCommand request, CancellationToken ct)
    {
        var period = new ExceptionPeriod(request.UserId, request.Start, request.End);
        await _repo.AddAsync(period, ct);
        return Result.Success();
    }
}
```

### b) Validators

**CreateExceptionPeriodCommandValidator.cs**

```csharp
public class CreateExceptionPeriodCommandValidator : AbstractValidator<CreateExceptionPeriodCommand>
{
    public CreateExceptionPeriodCommandValidator()
    {
        RuleFor(x => x.End).GreaterThanOrEqualTo(x => x.Start);
    }
}
```

---

## 3. Infraestrutura (Infrastructure Layer)

### a) Configurações EF Core

**HolidayConfiguration.cs**

```csharp
public class HolidayConfiguration : IEntityTypeConfiguration<Holiday>
{
    public void Configure(EntityTypeBuilder<Holiday> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Date).IsRequired();
        builder.Property(h => h.Description).HasMaxLength(100);
    }
}
```

**ExceptionPeriodConfiguration.cs**

```csharp
public class ExceptionPeriodConfiguration : IEntityTypeConfiguration<ExceptionPeriod>
{
    public void Configure(EntityTypeBuilder<ExceptionPeriod> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.UserId).IsRequired();
        builder.Property(e => e.StartDate).IsRequired();
        builder.Property(e => e.EndDate).IsRequired();
    }
}
```

### b) Repositórios

**HolidayRepository.cs**

```csharp
public class HolidayRepository : IHolidayRepository
{
    private readonly DbContext _context;
    public HolidayRepository(DbContext context) => _context = context;

    public async Task AddAsync(Holiday holiday, CancellationToken ct) =>
        await _context.Set<Holiday>().AddAsync(holiday, ct);
    // ...outros métodos...
}
```

**ExceptionPeriodRepository.cs**

```csharp
public class ExceptionPeriodRepository : IExceptionPeriodRepository
{
    private readonly DbContext _context;
    public ExceptionPeriodRepository(DbContext context) => _context = context;

    public async Task AddAsync(ExceptionPeriod period, CancellationToken ct) =>
        await _context.Set<ExceptionPeriod>().AddAsync(period, ct);
    // ...outros métodos...
}
```

### c) Utilitário de Parsing

**FileParser.cs**

```csharp
public class FileParser : IFileParser
{
    public async Task<IEnumerable<Holiday>> ParseHolidaysAsync(IFormFile file)
    {
        // Exemplo: parsing CSV
        using var reader = new StreamReader(file.OpenReadStream());
        var holidays = new List<Holiday>();
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            var parts = line.Split(',');
            if (DateTime.TryParse(parts[0], out var date))
                holidays.Add(new Holiday(date, parts[1]));
        }
        return holidays;
    }
}
```

---

## 4. API Layer

### a) Controllers

**HolidaysController.cs**

```csharp
[ApiController]
[Route("api/holidays")]
public class HolidaysController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("import")]
    public async Task<IActionResult> Import([FromForm] ImportHolidaysCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }
}
```

**ExceptionPeriodsController.cs**

```csharp
[ApiController]
[Route("api/exception-periods")]
public class ExceptionPeriodsController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExceptionPeriodCommand command)
    {
        var result = await _mediator.Send(command);
        return result.IsSuccess ? Ok() : BadRequest(result.Errors);
    }
}
```

---

## 5. Testes

### a) Testes de Domínio

**HolidayTests.cs**

```csharp
public class HolidayTests
{
    [Fact]
    public void Should_Create_Holiday_With_Valid_Data()
    {
        var holiday = new Holiday(new DateTime(2025, 12, 25), "Natal");
        Assert.Equal("Natal", holiday.Description);
        Assert.Equal(new DateTime(2025, 12, 25), holiday.Date);
    }
}
```

**ExceptionPeriodTests.cs**

```csharp
public class ExceptionPeriodTests
{
    [Fact]
    public void Should_Throw_When_EndDate_Before_StartDate()
    {
        Assert.Throws<ArgumentException>(() =>
            new ExceptionPeriod(Guid.NewGuid(), DateTime.Today, DateTime.Today.AddDays(-1)));
    }
}
```

### b) Testes de Application/Handlers

**ImportHolidaysHandlerTests.cs**

```csharp
public class ImportHolidaysHandlerTests
{
    [Fact]
    public async Task Should_Import_Holidays_From_File()
    {
        // Arrange
        // ...mock file, parser, repo...
        // Act
        // ...call handler...
        // Assert
        // ...verify holidays added...
    }
}
```

---

## 6. Documentação

- Atualizar:
  - domain-model.md (novas entidades e relacionamentos)
  - alarms.endpoints.md (novos endpoints)
  - frontend-development.md (UI de upload e gestão)
  - `docs/business/Roadmap MVP - Smart Alarm.md` (novos fluxos)

---

## 7. Observações Finais

- Implemente de forma incremental: domínio e testes, depois infraestrutura, depois API, depois integração.
- Garanta cobertura de testes ≥ 80% para código crítico.
- Valide e trate erros de upload/parsing.
- Siga os padrões de Clean Architecture, SOLID e documentação do projeto.
