namespace VPay.Ols.Processor.Data.Sql;

public sealed class ConnectionConfig
{
    public string Hostname { get; set; }
    public string Database { get; set; }

    public string Username { get; set; }

    public string Password { get; set; }

    public bool IntegratedSecurity { get; set; }

    public string BaseConnectionString { get; set; }

    /// <summary>
    /// The time in seconds to wait for the command to execute. The default is 30 seconds.
    /// </summary>
    public int? DefaultCommandTimeout { get; set; }

    public ConnectionConfig()
    {
        Hostname = string.Empty;
        Database = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        BaseConnectionString = string.Empty;        
    }
}
