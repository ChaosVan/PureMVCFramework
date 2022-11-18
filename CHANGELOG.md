# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.1.6] - 2022-11-18
- 改进：删除Entity时，优化GameObject处理方式
- 改进：WorldTime更新顺序
- 改进：移除不需要的依赖包
- 改进：Timer
- 改进：GenericSystemBase生成的代码
## [1.1.4] - 2022-11-01
- 改进：EnableUpdate可以自定义TaskName
- 改进：通过反射创建的类，添加Preserve属性
- 改进：修改SceneLayer的排序
- 改进：移除Entity时，不对GameObject判空
## [1.1.3] - 2022-10-09
- 改进：增加通过Archetype增加组件的接口
- 改进：增加通过EntityQuery删除组件的接口
- 改进：修改音乐音效Provider的属性
- 改进：增加固定帧率的Group
## [1.1.2] - 2022-10-06
- 改进：Entity的一些接口
- 改进：TimerTask增加调试用的Name
- 改进：SetLayer增加递归参数
## [1.1.1] - 2022-09-21
- 修复：使用自定义的RateManager后，World.Time.DeltaTime不对的问题
- 修复：删除Entity后，组件没有回收的问题
- 改进：优化EntityDebugger调试界面
## [1.1.0] - 2022-09-15
- 重构：Entity
- 改进：Editor下的一些接口
## [1.0.0-preview.21] - 2022-07-07
- 改进：ReferencePool接口
- 改进：AudioManager接口
- 改进：DomainReload
- 改进：LoadScene回调
- 修改：UIWindowParam修改为class
- 改进：增加package.json的Samples
## [1.0.0-preview.15] - 2022-02-28
- 改进：增加一个EntitManager.DestroyEntity的接口，支持自定义处理gameObject
## [1.0.0-preview.14] - 2022-01-18
- 改进：System更新的安全性
- 增加：SetLayer接口
## [1.0.0-preview.13] - 2021-12-31
- 改进：IInitializeable的接口增加参数
- 改进：修改相应的代码段和生成代码的逻辑
- 修复：引用池二次使用时没有执行构造函数的问题
## [1.0.0-preview.10] - 2021-12-28
- 改进：重新生成System代码
- 更新：增加域重载（DomainReload）特性
- 修复：EntityManager在LoadGameObject时可能产生的Bug

## [1.0.0-preview.9] - 2021-12-22
- 改进：System在OnRecycle中清空实体和组件集合

## [1.0.0-preview.8] - 2021-12-21
- 改进：EntityHash类型改为long
- 改进：EntityManager增加获取所有Entity和ComponentData的接口

## [1.0.0-preview.7] - 2021-12-20
- 修复：由于添加删除组件导致的系统更新时序问题
- 改进：修改Entity注入时机
- 改进：UILoopScrollRect兼容2021.2的接口改动

## [1.0.0-preview.6] - 2021-12-08
- 改进：EntityManager增加删除所有Entity的接口

## [1.0.0-preview.5] - 2021-12-08
- 新增：EntityManager添加纯数据模式
- 新增：Editor中纯数据模式下绘制LocalToWorld的自身坐标系
- 改进：受对象池g康的资源，在实例化时会自动添加DontDestroyOnLoad

## [1.0.0-preview.4] - 2021-12-06
- 改进：重复添加组件时会抛出异常

## [1.0.0-preview.3] - 2021-11-26
- 新增：生成EntityComponentHash的辅助工具
- 改进：VirtualWorld中添加TimePerFrame的属性
- 改进：System中添加Initialize和Recycle的接口

