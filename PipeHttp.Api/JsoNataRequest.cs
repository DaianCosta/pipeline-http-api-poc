namespace PipeHttp.Api;

public class JsoNataRequest
{
    public string Instruction { get; set; } = "";
    public required Dictionary<string, object> Data { get; set; }
}
