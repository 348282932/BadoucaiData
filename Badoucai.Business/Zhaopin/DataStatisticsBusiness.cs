using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Badoucai.EntityFramework.PostgreSql.AIF_DB;
using Badoucai.EntityFramework.PostgreSql.BadoucaiAliyun_DB;

namespace Badoucai.Business.Zhaopin
{
    public class DataStatisticsBusiness
    {
        public void StatisticsByArea()
        {
            var cityDictionary = new Dictionary<int, string>
            {
                #region 市

                { 90287536, "北京市" },
                { 90487536, "上海市" },
                { 90617636, "广州市" },
                { 90617836, "深圳市" },
                { 90517636, "合肥市" },
                { 90677536, "重庆市" },
                { 90527636, "福州市" },
                { 90797636, "兰州市" },
                { 90697636, "贵阳市" },
                { 90637636, "海口市" },
                { 90307636, "石家庄市" },
                { 90587636, "郑州市" },
                { 90407636, "哈尔滨市" },
                { 90597636, "武汉市" },
                { 90607636, "长沙市" },
                { 90397636, "长春市" },
                { 90497636, "南京市" },
                { 90537636, "南昌市" },
                { 90387636, "沈阳市" },
                { 90807636, "西宁市" },
                { 90547636, "济南市" },
                { 90317636, "太原市" },
                { 90787636, "西安市" },
                { 90687636, "成都市" },
                { 90297536, "天津市" },
                { 90707636, "昆明市" },
                { 90507636, "杭州市" },
                { 90627636, "南宁市" },
                { 90327636, "呼和浩特市" },
                { 90817636, "银川市" },
                { 90717636, "拉萨市" },
                { 90827636, "乌鲁木齐市" },
                { 90997536, "澳门特别行政区" },
                { 90987536, "香港特别行政区" },

                #endregion
            };

            var provinceDictionary = new Dictionary<int, string>
            { 
                #region 省

                { 90617536, "广东省" },
                { 90517536, "安徽省" },
                { 90527536, "福建省" },
                { 90797536, "甘肃省" },
                { 90697536, "贵州省" },
                { 90637536, "海南省" },
                { 90307536, "河北省" },
                { 90587536, "河南省" },
                { 90407536, "黑龙江省" },
                { 90597536, "湖北省" },
                { 90607536, "湖南省" },
                { 90397536, "吉林省" },
                { 90497536, "江苏省" },
                { 90537536, "江西省" },
                { 90387536, "辽宁省" },
                { 90807536, "青海省" },
                { 90547536, "山东省" },
                { 90317536, "山西省" },
                { 90787536, "陕西省" },
                { 90687536, "四川省" },
                { 90707536, "云南省" },
                { 90507536, "浙江省" },
                { 90627536, "广西壮族自治区" },
                { 90327536, "内蒙古自治区" },
                { 90817536, "宁夏回族自治区" },
                { 90717536, "西藏自治区" },
                { 90827536, "新疆维吾尔自治区" }

                #endregion
            };

            var sb = new StringBuilder();

            foreach (var area in cityDictionary)
            {
                var count = 0;

                using (var bdb = new BadoucaiAliyunDBEntities())
                {
                    bdb.Database.CommandTimeout = 600;

                    count += bdb.CoreResumeSummary.AsNoTracking().Count(c => c.CurrentResidence == area.Key);
                
                    using (var adb = new AIFDBEntities())
                    {
                        var areaList = adb.BaseAreaBDC.AsNoTracking().Where(w => w.PId == area.Key).ToList();

                        if (areaList.Count == 1)
                        {
                            var pid = areaList[0].Id;

                            areaList = adb.BaseAreaBDC.AsNoTracking().Where(w => w.PId == pid).ToList();
                        }

                        count += areaList.Sum(item => bdb.CoreResumeSummary.AsNoTracking().Count(c => c.CurrentResidence == item.Id));
                    }
                }

                sb.AppendLine($"{area.Value}\t{count}");
            }

            foreach (var province in provinceDictionary)
            {
                var count = 0;

                using (var adb = new AIFDBEntities())
                {
                    var cityList = adb.BaseAreaBDC.AsNoTracking().Where(w => w.PId == province.Key).ToList();

                    if (cityList.Count == 1)
                    {
                        var pid = cityList[0].Id;

                        cityList = adb.BaseAreaBDC.AsNoTracking().Where(w => w.PId == pid).ToList();
                    }

                    foreach (var area in cityList)
                    {
                        using (var bdb = new BadoucaiAliyunDBEntities())
                        {
                            count += bdb.CoreResumeSummary.AsNoTracking().Count(c => c.CurrentResidence == area.Id);

                            var areaList = adb.BaseAreaBDC.AsNoTracking().Where(w => w.PId == area.Id).ToList();

                            if (areaList.Count == 1)
                            {
                                var pid = areaList[0].Id;

                                areaList = adb.BaseAreaBDC.AsNoTracking().Where(w => w.PId == pid).ToList();
                            }

                            count += areaList.Sum(item => bdb.CoreResumeSummary.AsNoTracking().Count(c => c.CurrentResidence == item.Id));
                        }
                    }
                }

                sb.AppendLine($"{province.Value}\t{count}");
            }

            File.AppendAllText(@"D:\统计.txt", sb.ToString());
        }
    }
}