using ClinicSlot.Application.Abstractions;
using ClinicSlot.Application.Contracts;
using ClinicSlot.Application.Exceptions;
using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Practitioners;
using Microsoft.Extensions.Logging;

namespace ClinicSlot.Application.Services;

/// <summary>Set-up operations: clinics, the services they offer, practitioners and time off.</summary>
public sealed class ClinicDirectoryService(
    IClinicRepository clinics,
    IClinicServiceRepository services,
    IPractitionerRepository practitioners,
    ITimeOffRepository timeOffs,
    IUnitOfWork unitOfWork,
    ILogger<ClinicDirectoryService> logger)
{
    public async Task<ClinicResponse> CreateClinicAsync(CreateClinicRequest request, CancellationToken cancellationToken)
    {
        var clinic = Clinic.Create(request.Name, request.TimeZone, request.SlotIntervalMinutes, request.ChangeCutoffHours);
        clinics.Add(clinic);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        Log.ClinicCreated(logger, clinic.Id, clinic.TimeZoneId);
        return clinic.ToResponse();
    }

    public async Task<ClinicResponse> GetClinicAsync(Guid clinicId, CancellationToken cancellationToken)
    {
        var clinic = await RequireClinicAsync(clinicId, cancellationToken);
        return clinic.ToResponse();
    }

    public async Task<ServiceResponse> AddServiceAsync(Guid clinicId, CreateServiceRequest request, CancellationToken cancellationToken)
    {
        await RequireClinicAsync(clinicId, cancellationToken);

        var service = ClinicService.Create(clinicId, request.Name, request.DurationMinutes, request.BufferMinutes);
        services.Add(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return service.ToResponse();
    }

    public async Task<IReadOnlyList<ServiceResponse>> ListServicesAsync(Guid clinicId, CancellationToken cancellationToken)
    {
        await RequireClinicAsync(clinicId, cancellationToken);
        var list = await services.ListByClinicAsync(clinicId, cancellationToken);
        return list.Select(s => s.ToResponse()).ToList();
    }

    public async Task<PractitionerResponse> AddPractitionerAsync(
        Guid clinicId,
        CreatePractitionerRequest request,
        CancellationToken cancellationToken)
    {
        await RequireClinicAsync(clinicId, cancellationToken);

        var practitioner = Practitioner.Create(clinicId, request.Name, request.WorkingHours.ToSchedule());
        practitioners.Add(practitioner);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return practitioner.ToResponse();
    }

    public async Task<PractitionerResponse> GetPractitionerAsync(Guid practitionerId, CancellationToken cancellationToken)
    {
        var practitioner = await RequirePractitionerAsync(practitionerId, cancellationToken);
        return practitioner.ToResponse();
    }

    public async Task<PractitionerResponse> ReplaceScheduleAsync(
        Guid practitionerId,
        ReplaceScheduleRequest request,
        CancellationToken cancellationToken)
    {
        var practitioner = await RequirePractitionerAsync(practitionerId, cancellationToken);
        practitioner.ReplaceSchedule(request.WorkingHours.ToSchedule());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return practitioner.ToResponse();
    }

    public async Task<TimeOffResponse> AddTimeOffAsync(
        Guid practitionerId,
        CreateTimeOffRequest request,
        CancellationToken cancellationToken)
    {
        await RequirePractitionerAsync(practitionerId, cancellationToken);

        var timeOff = TimeOff.Create(practitionerId, request.Start, request.End, request.Reason);
        timeOffs.Add(timeOff);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return timeOff.ToResponse();
    }

    private async Task<Clinic> RequireClinicAsync(Guid clinicId, CancellationToken cancellationToken) =>
        await clinics.FindAsync(clinicId, cancellationToken) ?? throw new NotFoundException("Clinic", clinicId);

    private async Task<Practitioner> RequirePractitionerAsync(Guid practitionerId, CancellationToken cancellationToken) =>
        await practitioners.FindAsync(practitionerId, cancellationToken)
        ?? throw new NotFoundException("Practitioner", practitionerId);
}
