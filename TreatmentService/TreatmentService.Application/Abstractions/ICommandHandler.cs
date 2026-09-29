namespace TreatmentService.Application.Abstractions;

public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : class, ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken cancellationToken);
}
