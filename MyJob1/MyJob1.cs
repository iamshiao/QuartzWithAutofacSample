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
        public void Execute(IJobExecutionContext context)
        {
            Console.WriteLine("【MyJob1】" + context.Trigger.Key.Name);
        }
    }
}
