using System.Threading.Tasks;
using TokenShield.Application.Dto.Admin;

namespace TokenShield.Application.Common.Interfaces;

public interface IProfilerRuleService
{
    Task<TestProfilerRuleResponse> TestRuleAsync(TestProfilerRuleRequest request);
}
