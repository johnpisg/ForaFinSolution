using Azure.Storage.Queues;
using System.Text.Json;
using ForaFinServices.Application.Dtos;

namespace FofaFin.Utils;
public class QueueService : IQueueService
{
    private readonly QueueClient _queueClient;

    public QueueService(string connectionString)
    {
        // 1. Inicializamos el cliente apuntando a la cola "import-requests"
        _queueClient = new QueueClient(connectionString, "import-requests", new QueueClientOptions
        {
            MessageEncoding = QueueMessageEncoding.Base64 // Muy importante para Azure Functions
        });
    }

    public async Task EnqueueImportRequestAsync(StartImportRequestDto data)
    {
        // 2. Asegurarse de que la cola existe (solo necesario la primera vez)
        await _queueClient.CreateIfNotExistsAsync();

        // 3. Serializar el objeto a JSON
        string messageBody = JsonSerializer.Serialize(data);

        // 4. Enviar a la cola
        // El primer parámetro es el contenido, el segundo es el tiempo de invisibilidad (null = inmediato)
        // El tercero es el Time To Live (null = 7 días por defecto)
        await _queueClient.SendMessageAsync(messageBody);
    }
}