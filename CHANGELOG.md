# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

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

