using Api.Dto;
using BenchmarkDotNet.Attributes;

namespace Api.Benchmarks;

[MemoryDiagnoser]
public class ResponseBenchmarks
{
    [Benchmark]
    public Response<string> CreateResponse()
    {
        return new Response<string>("ok", "mensagem");
    }

    [Benchmark]
    public PagedResponse<List<string>> CreatePagedResponse()
    {
        return new PagedResponse<List<string>>(
            data: ["a", "b", "c"],
            totalCount: 3,
            currentPage: 1,
            pageSize: 10,
            message: "Sucesso"
        );
    }
}