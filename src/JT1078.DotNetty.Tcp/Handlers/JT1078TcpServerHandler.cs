﻿using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using System;
using Microsoft.Extensions.Logging;
using JT1078.DotNetty.Core.Enums;
using JT1078.DotNetty.Core.Services;
using JT1078.Protocol;
using JT1078.DotNetty.Core.Interfaces;
using JT1078.DotNetty.Core.Session;
using JT1078.DotNetty.Core.Metadata;

namespace JT1078.DotNetty.Tcp.Handlers
{
    /// <summary>
    /// JT1078 服务端处理程序
    /// </summary>
    internal class JT1078TcpServerHandler : SimpleChannelInboundHandler<byte[]>
    {
        private readonly JT1078TcpSessionManager SessionManager;

        private readonly JT1078AtomicCounterService AtomicCounterService;

        private readonly ILogger<JT1078TcpServerHandler> logger;

        private readonly IJT1078TcpMessageHandlers handlers;

        private readonly IJT1078SourcePackageDispatcher sourcePackageDispatcher;

        public JT1078TcpServerHandler(
            IJT1078SourcePackageDispatcher sourcePackageDispatcher,
            IJT1078TcpMessageHandlers handlers,
            ILoggerFactory loggerFactory,
            JT1078AtomicCounterServiceFactory atomicCounterServiceFactory,
            JT1078TcpSessionManager sessionManager)
        {
            this.sourcePackageDispatcher = sourcePackageDispatcher;
            this.handlers = handlers;
            this.SessionManager = sessionManager;
            this.AtomicCounterService = atomicCounterServiceFactory.Create(JT1078TransportProtocolType.tcp);
            logger = loggerFactory.CreateLogger<JT1078TcpServerHandler>();
        }


        protected override void ChannelRead0(IChannelHandlerContext ctx, byte[] msg)
        {
            try
            {
                sourcePackageDispatcher.SendAsync(msg);
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("accept package success count<<<" + AtomicCounterService.MsgSuccessCount.ToString());
                    logger.LogTrace("accept msg <<< " + ByteBufferUtil.HexDump(msg));
                }
                JT1078Package package = JT1078Serializer.Deserialize(msg);
                AtomicCounterService.MsgSuccessIncrement();
                SessionManager.TryAdd(package.SIM, ctx.Channel);
                handlers.Processor(new JT1078Request(package, msg));
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("accept package success count<<<" + AtomicCounterService.MsgSuccessCount.ToString());
                }
            }
            catch (Exception ex)
            {
                AtomicCounterService.MsgFailIncrement();
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError("accept package fail count<<<" + AtomicCounterService.MsgFailCount.ToString());
                    logger.LogError(ex, "accept msg<<<" + ByteBufferUtil.HexDump(msg));
                }
            }
        }
    }
}
