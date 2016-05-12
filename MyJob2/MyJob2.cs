using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Idv.CircleHsiao.MyJob2
{
    public class MyJob2 : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            Console.WriteLine("【MyJob2】" + context.Trigger.Key.Name);
        }
    }
}
