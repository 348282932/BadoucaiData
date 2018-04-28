using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Badoucai.EntityFramework.MySql;

namespace Badoucai.Business.Zhaopin
{
    public class CheckCodeBusiness
    {
        private static readonly Dictionary<int, DateTime> dictionary = new Dictionary<int, DateTime>();

        public static int waitCount;

        public Dictionary<string, int> GetTodayRank()
        {
            using (var db = new MangningXssDBEntities())
            {
                return db.ZhaopinCheckCode
                    .Where(w => w.CompleteTime > DateTime.Today && w.Status == 2)
                    .GroupBy(s => new { s.HandleUser, s.CompleteTime })
                    .Select(s=> new { s.Key.HandleUser, s.Key.CompleteTime })
                    .ToList()
                    .GroupBy(g => new { g.HandleUser })
                    .Select(s => new { s.Key, Count = s.Count() })
                    .ToDictionary(a => a.Key.HandleUser, b => b.Count);

            }
        }

        public Dictionary<string, int> GetTotalRank()
        {
            using (var db = new MangningXssDBEntities())
            {
                return db.ZhaopinCheckCode
                    .Where(w => w.Status == 2)
                    .GroupBy(s => new { s.HandleUser, s.CompleteTime })
                    .Select(s => new { s.Key.HandleUser, s.Key.CompleteTime })
                    .ToList()
                    .GroupBy(g => new { g.HandleUser })
                    .Select(s => new { s.Key, Count = s.Count() })
                    .ToDictionary(a => a.Key.HandleUser, b => b.Count);
            }
        }

        /// <summary>
        /// 重置状态
        /// </summary>
        public void ResetStatus()
        {
            using (var db = new MangningXssDBEntities())
            {
                var list = db.ZhaopinCheckCode.Where(a => a.Status == 1).ToList();

                foreach (var item in list)
                {
                    item.Status = 0;
                }

                var procedures = db.ZhaopinCleaningProcedure.Where(a => a.IsOnline).ToList();

                foreach (var item in procedures)
                {
                    item.IsOnline = false;
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// 启动清洗程序
        /// </summary>
        /// <param name="model"></param>
        public void CleaningStart(ZhaopinCleaningProcedure model)
        {
            using (var db = new MangningXssDBEntities())
            {
                var cleaningProcedure = db.ZhaopinCleaningProcedure.FirstOrDefault(f => f.Id == model.Id);

                if (cleaningProcedure == null)
                {
                    db.ZhaopinCleaningProcedure.Add(model);
                }
                else
                {
                    cleaningProcedure.StartTime = DateTime.UtcNow;

                    cleaningProcedure.IsOnline = true;
                }

                db.SaveChanges();
            }
        }

        /// <summary>
        /// 关闭清洗程序
        /// </summary>
        /// <param name="account"></param>
        public void CleaningEnd(string account)
        {
            using (var db = new MangningXssDBEntities())
            {
                var cleaningProcedure = db.ZhaopinCleaningProcedure.FirstOrDefault(f => f.Account == account);

                if (cleaningProcedure != null)
                {
                    cleaningProcedure.IsOnline = false;

                    db.SaveChanges();
                }
            }
        }

        /// <summary>
        /// 插入验证码
        /// </summary>
        /// <param name="checkCode"></param>
        public int InserCheckCode(ZhaopinCheckCode checkCode)
        {
            using (var db = new MangningXssDBEntities())
            {
                var checkModel = db.ZhaopinCheckCode.FirstOrDefault(a => a.Account == checkCode.Account && (a.Status == 0 || a.Status == 1));

                if(checkModel != null) return checkModel.Id;

                checkCode = db.ZhaopinCheckCode.Add(checkCode);

                db.SaveChanges();

                return checkCode.Id;
            }
        }

        /// <summary>
        /// 查询验证状态
        /// </summary>
        /// <returns></returns>
        public int GetProcessingCount()
        {
            try
            {
                using (var db = new MangningXssDBEntities())
                {
                    return db.ZhaopinCheckCode.Count(c => c.Status == 1);
                }
            }
            catch(Exception)
            {
                return 0;
            }
            
        }

        /// <summary>
        /// 获取客户端状态
        /// </summary>
        /// <returns></returns>
        public int GetWaitCount()
        {
            try
            {
                using (var db = new MangningXssDBEntities())
                {
                    //var list = db.ZhaopinCheckCode.Where(w => w.Status == 0 || w.Status == 1).Join(db.ZhaopinCleaningProcedure, a => a.CleaningId, b => b.Id, (a, b) => new {b.Account, a.Status}).Distinct().ToList();

                    //var lista = list.GroupBy(g => new { Account = g.Account.Substring(0, g.Account.IndexOf("_", StringComparison.Ordinal)), g.Status }).Select(s=>s).ToList();

                    //var arr = lista.Where(w => w.Key.Status == 1).Select(s=>s.Key.Account);

                    //waitCount = lista.Count(w => arr.All(a => a != w.Key.Account));

                    //return waitCount;

                    waitCount = db.ZhaopinCheckCode.Count(c => c.Status == 0);

                    return waitCount;
                }
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// 获取验证码
        /// </summary>
        /// <returns></returns>
        public ZhaopinCheckCode GetCheckCode()
        {
            if (waitCount == 0) return null;

            using (var db = new MangningXssDBEntities())
            {
                var checkCode = db.ZhaopinCheckCode.FirstOrDefault(f => f.Status == 0);

                if (checkCode == null) return null;

                checkCode.Status = 1;

                db.SaveChanges();

                dictionary.Add(checkCode.Id, DateTime.Now.AddMinutes(1));

                waitCount--;

                return checkCode;
            }
        }

        /// <summary>
        /// 验证结果
        /// </summary>
        /// <param name="checkCodeId"></param>
        /// <param name="status">1.成功 2.失败</param>
        /// <param name="account"></param>
        /// <param name="handleUser"></param>
        /// <returns></returns>
        public string[] CheckResult(int checkCodeId, short status, string account, string handleUser)
        {
            if (status == 1)
            {
                using (var db = new MangningXssDBEntities())
                {
                    var user = account.Substring(0, account.IndexOf("_", StringComparison.Ordinal));

                    var checkCodes = db.ZhaopinCheckCode.Where(w => w.Account.StartsWith(user) && (w.Status == 0 || w.Status == 1)).ToList();

                    var date = DateTime.Now;

                    foreach (var checkCode in checkCodes)
                    {
                        dictionary.Remove(checkCode.Id);

                        checkCode.Status = 2;

                        checkCode.HandleUser = handleUser;

                        checkCode.CompleteTime = date;
                    }

                    db.SaveChanges();

                    return checkCodes.Select(s=>s.Account).ToArray();
                }
            }

            if (status == 2)
            {
                if (!dictionary.ContainsKey(checkCodeId)) return null;

                dictionary[checkCodeId] = DateTime.Now.AddMinutes(1);
            }

            return null;
        }

        /// <summary>
        /// 验证结果
        /// </summary>
        /// <param name="checkCodeId"></param>
        /// <param name="status">1.成功 2.失败</param>
        /// <param name="cleaningId"></param>
        /// <returns></returns>
        public void LoginCheckResult(int checkCodeId, short status, short cleaningId)
        {
            if (status == 1)
            {
                using (var db = new MangningXssDBEntities())
                {

                    var checkCode = db.ZhaopinCheckCode.FirstOrDefault(f => f.Id == checkCodeId);

                    var date = DateTime.Now;

                    if(checkCode == null) return;

                    dictionary.Remove(checkCode.Id);

                    checkCode.Status = 2;

                    checkCode.CompleteTime = date;

                    db.SaveChanges();
                }
            }

            if (status == 2)
            {
                if (!dictionary.ContainsKey(checkCodeId)) return;

                dictionary[checkCodeId] = DateTime.Now.AddMinutes(1);
            }
        }

        /// <summary>
        /// 启动验证计时器
        /// </summary>
        /// <param name="checkCodeId"></param>
        public void StartTimer()
        {
            while (true)
            {
                Thread.Sleep(1000);

                var list = dictionary.Where(w => DateTime.Now.Subtract(w.Value).Milliseconds > 0).ToList();

                using (var db = new MangningXssDBEntities())
                {
                    foreach (var item in list)
                    {
                        var checkCode = db.ZhaopinCheckCode.FirstOrDefault(f => f.Id == item.Key);

                        if (checkCode == null) return;

                        checkCode.Status = 0;

                        dictionary.Remove(item.Key);

                        db.SaveChanges();
                    }
                }
            }
        }
    }
}