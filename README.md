# PureCat
[![Build status](https://ci.appveyor.com/api/projects/status/yimjei2as70cw319)](https://ci.appveyor.com/project/chinaboard/purecat)

Cat常见问题：[wiki](https://github.com/chinaboard/PureCat/wiki/Cat%E7%AE%80%E5%8D%95%E6%96%87%E6%A1%A3)

Cat.Net客户端，参考了点评原生客户端和Jasmine的部分实现


开发环境VS2015，低于此版本的VS会提示语法错误

##已实现的功能
1、Metrics

2、Heartbeat

3、上下文串联(Context)

4、DoTransaction封装事务


##测试中的功能
1、ForkedTransaction

2、TaggedTransaction


##开发中的功能

1、客户端路由负载均衡

2、多线程发送log
