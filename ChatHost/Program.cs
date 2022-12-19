using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;

namespace ChatHost
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var host = new ServiceHost(typeof(wcf_chat.ServiceChat)))    // вводим такую конструкцию
            {
                host.Open();                                                    // открываем хост
                Console.WriteLine("Хост стартовал!");
                Console.ReadLine();     // т.к. это консольное приложение, чтобы оно не закрылось, организуем ожидание ввода с клавиатуры.
            }
        }
    }
}
