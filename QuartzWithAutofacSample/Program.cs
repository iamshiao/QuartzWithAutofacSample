using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Idv.CircleHsiao.QuartzWithAutofacSample
{
    class Program
    {
        static void Main(string[] args)
        {
            #region Sample data
            JobSetting js = new JobSetting
            {
                Name = "MyJob1",
                Group = "Group1",
                Namespace = "Idv.CircleHsiao.MyJob1",
                TriggerNames = new List<string>() { "Trigger1" }
            };

            JobSetting js2 = new JobSetting
            {
                Name = "MyJob2",
                Group = "Group2",
                Namespace = "Idv.CircleHsiao.MyJob2",
                TriggerNames = new List<string>() { "Trigger2", "Trigger3" }
            };

            TriggerSetting ts = new TriggerSetting
            {
                Name = "Trigger1",
                Group = "Group1",
                IntervalSec = 3,
                RepeatCount = 0,
                WillRepeatForever = true,
                StartAt = DateTime.Now.AddSeconds(10).ToString("yyyy-MM-dd hh:mm:ss")
            };

            TriggerSetting ts2 = new TriggerSetting
            {
                Name = "Trigger2",
                Group = "Group2",
                IntervalSec = 5,
                RepeatCount = 2,
                WillRepeatForever = false,
                StartAt = DateTime.Now.AddSeconds(10).ToString("yyyy-MM-dd hh:mm:ss")
            };

            TriggerSetting ts3 = new TriggerSetting
            {
                Name = "Trigger3",
                Group = "Gear3",
                IntervalSec = 1,
                RepeatCount = 10,
                WillRepeatForever = false,
                StartAt = DateTime.Now.AddSeconds(10).ToString("yyyy-MM-dd hh:mm:ss")
            };

            ScheduleSetting schSetting = new ScheduleSetting
            {
                JobSettings = new List<JobSetting> { js, js2 },
                TriggerSettings = new List<TriggerSetting> { ts, ts2, ts3 }
            };
            #endregion

            // Read dlls
            DirectoryInfo dirToScan = new DirectoryInfo(@"../../../");
            FileInfo[] files = dirToScan.GetFiles("*.dll", SearchOption.AllDirectories);
            List<string> dlls = files.Select(f => f.FullName).ToList();

            // Search and collect classes implemented IJob
            List<Type> typesImplIJob = new List<Type>();
            dlls.ForEach(dll =>
            {
                Assembly asm = Assembly.LoadFrom(dll);
                if (asm != null)
                    foreach (Type eachType in asm.GetExportedTypes())
                        if (eachType.GetInterface("IJob") != null && // must impl IJob
                            schSetting.JobSettings.Any(j => j.Namespace == eachType.Namespace) && // also belongs to our solu
                            !typesImplIJob.Any(tij => tij.Name == eachType.Name)) // and not repeated
                            typesImplIJob.Add(eachType);
            });

            ISchedulerFactory sf = new StdSchedulerFactory();
            var sched = sf.GetScheduler();
            typesImplIJob.ForEach(typeImplIJob =>
            {
                #region Create JobDetail by sample data

                JobSetting jSett = schSetting.JobSettings.FirstOrDefault(j => j.Name == typeImplIJob.Name);
                if (jSett == null) // Check type from dll existed in sample data as well
                {
                    Console.WriteLine("Can't find " + typeImplIJob.Name + " from sample data setting.");
                    return;
                }

                IJobDetail job = JobBuilder.Create(typeImplIJob)
                    .WithIdentity(jSett.Name, jSett.Group)
                    .Build();
                #endregion

                #region Create Trigger by sample data

                List<TriggerSetting> tSetts = schSetting.TriggerSettings.Where(
                    t => jSett.TriggerNames.Contains(t.Name)).ToList();

                tSetts.ForEach(tSett =>
                {
                    TriggerBuilder tb = TriggerBuilder.Create();

                    tb.WithIdentity(tSett.Name, tSett.Group)
                    .ForJob(jSett.Name, jSett.Group) // pre assign belongs to which job
                    .WithSimpleSchedule(ss =>
                    {
                        ss = ss.WithIntervalInSeconds(tSett.IntervalSec)
                            .WithRepeatCount(tSett.RepeatCount);
                        if (tSett.WillRepeatForever)
                            ss.RepeatForever();
                    });

                    if (string.IsNullOrWhiteSpace(tSett.StartAt))
                        tb.StartNow();
                    else
                    {
                        DateTime when = DateTime.Parse(tSett.StartAt);
                        if (DateTime.Now < when) // setting datetime hasn't expired
                            tb.StartAt(DateTime.Parse(tSett.StartAt));
                        else // if expired
                            // and the time today has passed as well
                            if (DiffDateOfSameTime(DateTime.Now, when) < DateTime.Now)
                                // fire at the same time tomorrow
                                tb.StartAt(DiffDateOfSameTime(DateTime.Now, when).AddDays(1));
                            else // fire at the time today
                                tb.StartAt(DiffDateOfSameTime(DateTime.Now, when));
                    }

                    ITrigger trigger = tb.Build();

                    // combine job & trigger
                    if (sched.CheckExists(trigger.JobKey))
                        sched.ScheduleJob(trigger);
                    else
                        sched.ScheduleJob(job, trigger);
                });
                #endregion
            });


            // start the schedule
            sched.Start();
            Thread.Sleep(TimeSpan.FromMinutes(3));

            // shut down the scheduler
            sched.Shutdown(true);
        }

        private static DateTime DiffDateOfSameTime(DateTime datePart, DateTime timePart)
        {
            return new DateTime(datePart.Year, datePart.Month, datePart.Day,
                timePart.Hour, timePart.Minute, timePart.Second, timePart.Millisecond);
        }
    }

    #region Classes for sample data
    public class ScheduleSetting
    {
        public List<JobSetting> JobSettings { get; set; }
        public List<TriggerSetting> TriggerSettings { get; set; }
    }

    public class JobSetting
    {
        public string Name { get; set; }
        public string Group { get; set; }
        // To identify our IJob impl from others(like from Quartz itself)
        public string Namespace { get; set; }
        public List<string> TriggerNames { get; set; }
    }

    public class TriggerSetting
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public int IntervalSec { get; set; }
        public int RepeatCount { get; set; }
        // if(WillRepeatForever) ignore RepeatCount
        public bool WillRepeatForever { get; set; }
        public string StartAt { get; set; }
    }
    #endregion
}
