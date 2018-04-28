using System;
using System.Collections.Generic;

public class Resume
{
    #region 成员

    public string Id { get; set; }

    /// <summary>
    /// 姓名
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 性别
    /// </summary>
    public char Gender { get; set; }

    /// <summary>
    /// 出生日期
    /// </summary>
    public DateTime Birthday { get; set; }

    /// <summary>
    /// 最早工作时间
    /// </summary>
    public DateTime WorkStarts { get; set; }

    /// <summary>
    /// 手机
    /// </summary>
    public long Cellphone { get; set; }

    /// <summary>
    /// 邮件
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// 现居地
    /// </summary>
    public int CurrentResidence { get; set; }

    /// <summary>
    /// 户籍地址
    /// </summary>
    public int RegisteredResidenc { get; set; }

    /// <summary>
    /// 学历
    /// </summary>
    public char Degree { get; set; }

    /// <summary>
    /// 修读专业
    /// </summary>
    public string Major { get; set; }

    /// <summary>
    /// 婚姻状态
    /// </summary>
    public char MaritalStatus { get; set; }

    /// <summary>
    /// 求职状态
    /// </summary>
    public char JobStatus { get; set; }

    public DateTime UpdateTime { get; set; }

    /// <summary>
    /// 求职意向
    /// </summary>
    public Intention Intention { get; set; }

    /// <summary>
    /// 教育经历
    /// </summary>
    public List<Education> Educations { get; set; }

    /// <summary>
    /// 工作经历
    /// </summary>
    public List<Work> Works { get; set; }

    /// <summary>
    /// 项目经历
    /// </summary>
    public List<Project> Projects { get; set; }

    /// <summary>
    /// 培训经历
    /// </summary>
    public List<Training> Trainings { get; set; }

    public Reference Reference { get; set; }

    #endregion
}

public class Intention
{
    public short Id { get; set; }
    /// <summary>
    /// 简历编号
    /// </summary>
    public string ResumeId { get; set; }

    /// <summary>
    /// 期望薪资
    /// </summary>
    public decimal MinimumSalary { get; set; }

    public decimal MaximumSalary { get; set; }

    /// <summary>
    /// 期望工作地点
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// 期望行业
    /// </summary>
    public string Industry { get; set; }

    /// <summary>
    /// 期望职能
    /// </summary>
    public string Function { get; set; }

    /// <summary>
    /// 到岗时间
    /// </summary>
    public char DutyTime { get; set; }

    /// <summary>
    /// 工作类型
    /// </summary>
    public char JobType { get; set; }

    /// <summary>
    /// 自我评价
    /// </summary>
    public string Evaluation { get; set; }
}

public class Education
{
    public short Id { get; set; }

    public string ResumeId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime Begin { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// 学校
    /// </summary>
    public string School { get; set; }

    /// <summary>
    /// 学历
    /// </summary>
    public char Degree { get; set; }

    /// <summary>
    /// 修读专业
    /// </summary>
    public string Major { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }
}

public class Work
{
    public short Id { get; set; }

    public string ResumeId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime Begin { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// 单位名称
    /// </summary>
    public string Company { get; set; }

    /// <summary>
    /// 所在行业
    /// </summary>
    public short Industry { get; set; }

    /// <summary>
    /// 公司规模
    /// </summary>
    public char Size { get; set; }

    /// <summary>
    /// 公司性质
    /// </summary>
    public char Nature { get; set; }

    /// <summary>
    /// 所属部门
    /// </summary>
    public string Department { get; set; }

    /// <summary>
    /// 所在岗位
    /// </summary>
    public string Position { get; set; }

    /// <summary>
    /// 工作描述
    /// </summary>
    public string Description { get; set; }
}

public class Project
{
    public short Id { get; set; }

    public string ResumeId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime Begin { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// 项目名称
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// 所在单位
    /// </summary>
    public string Company { get; set; }

    /// <summary>
    /// 项目描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 职责描述
    /// </summary>
    public string Duty { get; set; }
}

public class Training
{
    public short Id { get; set; }

    public string ResumeId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime Begin { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    public DateTime End { get; set; }

    /// <summary>
    /// 培训机构
    /// </summary>
    public string Institution { get; set; }

    /// <summary>
    /// 培训地点
    /// </summary>
    public string Location { get; set; }

    /// <summary>
    /// 培训课程
    /// </summary>
    public string Course { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; set; }
}

public class Reference
{
    public string Id { get; set; }
    /// <summary>
    /// 简历来源
    /// </summary>
    public string Source { get; set; }

    public string Tag { get; set; }

    public string ResumeId { get; set; }

    public DateTime UpdateTime { get; set; }
    /// <summary>
    /// 模板类型 JSON/HTML……
    /// </summary>
    public string Template { get; set; }

    public bool HasContract { get; set; }

    public List<ReferenceMapping> Mapping { get; set; }

}

public class ReferenceMapping
{
    /// <summary>
    /// 引用ID
    /// </summary>
    public string Id { get; set; }
    /// <summary>
    /// 信息扩展键
    /// </summary>
    public string Key { get; set; }
    /// <summary>
    /// 信息扩展值
    /// </summary>
    public string Value { get; set; }
    /// <summary>
    /// 简历来源
    /// </summary>
    public string Source { get; set; }
}

