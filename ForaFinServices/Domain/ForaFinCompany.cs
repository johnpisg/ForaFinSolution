namespace ForaFinServices.Domain;

public class ForaFinCompany
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<ForaFinCompanyIncomeInfo> IncomeInfos { get; set; } = new List<ForaFinCompanyIncomeInfo>();
}

public class ForaFinCompanyIncomeInfo
{
    public int Id { get; set; }
    public int Year { get; set; }
    public decimal Income { get; set; }
    public int CompanyId { get; set; }
    public ForaFinCompany Company { get; set; }
}

