using ForaFinServices.Application.Dtos;

public interface IQueueService
{
   Task EnqueueImportRequestAsync(StartImportRequestDto data);
}