using System.ServiceModel;      // нужен для использования OperationContext

namespace wcf_chat
{
    class ServerUser
    {
        public int ID { get; set; }
    
        public string Name { get; set; }

        public OperationContext operationContext { get; set; }
    }
}
