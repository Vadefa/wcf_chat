using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace wcf_chat
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class ServiceChat : IServiceChat
    {
        List<ServerUser> users = new List<ServerUser>();    // список объектов ServerUser
        int nextId = 1;                                     // далее нам нужно присваивать ID.


        public int Connect(string name)
        {

                ServerUser user = new ServerUser()
                {
                    ID = nextId,
                    Name = name,
                    operationContext = OperationContext.Current
                };
                nextId++;
                SendMsg(" " + user.Name + "подключился к чату!", 0);
                users.Add(user);
                return user.ID;
        }

        public void Disconnect(int id)
        {
            // используем неявно типизированную переменную var, при этом используется LINQ
            var user = users.FirstOrDefault(i => i.ID == id);
            // если там такого юзера не будет, то в переменной будет значение null.
            if (user != null)
            {
                users.Remove(user);
                SendMsg(" " + user.Name + " покинул чат!", 0);
            }
        }

        public void SendMsg(string msg, int id)
        {
            foreach (var item in users)
            {
                string answer = DateTime.Now.ToShortTimeString();   // вернули время текущего сообщения
                var user = users.FirstOrDefault(i => i.ID == id);
                if (user != null)
                {
                    answer += " " + user.Name + ": ";
                }
                answer += msg;

                item.operationContext.GetCallbackChannel<IServerChatCallback>().MsgCallback(answer);

            }
        }
    }
}
