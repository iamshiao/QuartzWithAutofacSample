using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Idv.CircleHsiao.MyJob1
{
    public class MyJob1 : IJob
    {
        public string Input { get; set; }

        //public MyJob1()
        //{
        //    Input = "MyJob1";
        //}

        public MyJob1(string input)
        {
            Input = input;
        }

        public void Execute(IJobExecutionContext context)
        {
            Console.WriteLine("【" + Input + "】" + context.Trigger.Key.Name);
        }
    }
}
