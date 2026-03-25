using MAFStudio.Backend.Abstractions;

namespace MAFStudio.Backend.Services
{
    /// <summary>
    /// 智能体运行时托管服务
    /// 负责启动后台任务，定期检查并清理空闲智能体
    /// </summary>
    public class AgentRuntimeHostedService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AgentRuntimeHostedService> _logger;

        public AgentRuntimeHostedService(
            IServiceProvider serviceProvider,
            ILogger<AgentRuntimeHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("智能体运行时托管服务启动中...");

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            var runtimeService = _serviceProvider.GetRequiredService<IAgentRuntimeService>();

            await runtimeService.StartIdleDetectionAsync(stoppingToken);
        }
    }
}
