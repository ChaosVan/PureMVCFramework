# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [1.0.0-preview.5] - 2021-12-08
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

