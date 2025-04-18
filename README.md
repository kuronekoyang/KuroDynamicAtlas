# KuroDynamicAtlas
Unity高性能ASTC动态图集方案

在古代，人们只能在静态图集中使用压缩纹理，一旦到了动态图集，只能老老实实的使用RGBA32。

后来随着科技发展，人们开始使用ComputeShader在运行时进行动态图集的压缩。不过再怎么优化压缩算法，这个计算开销是避不掉的。

本方案对资产生产流程做了微小的一点改变，即可避免运行时的压缩计算开销。如果想兼容更广泛的机型，也可以轻松改成其他基于Block的压缩纹理方案。

## 工作原理

ASTC是一种基于Block的压缩纹理算法，Block的大小可以是4x4、6x6或者其他，但是一个Block的数据大小是固定的128bit。

我们在使用动态图集时，最主要的目的是把散图放到图集纹理中。传统做法都是基于像素去处理的，这种做法易于理解，但是与压缩纹理的模式是冲突的。

我们只需要改变一下思路，基于Block去处理，这样就不需要进行计算，只需要单纯的拷贝即可。

而基于Block处理，就需要让Block的像素尺寸是散图尺寸和图集尺寸的公约数。

例如6x6的Block，散图可以是126x126、132x132，但不能是128x128。图集可以是1020x1020、1026x1026，但不能是1024x1024。

所以我们就需要将散图尺寸扩大到Block的倍数，并压缩成ASTC格式，再导入到Unity中。而由于我们本来就需要给散图增加Padding，所以可以一起放在这一步中执行。

在将散图拷贝到图集中时，如果目标设备支持CopyTexture，那么我们可以使用Graphics.CopyTexture，否则的话在GetPixelData之后手动Copy。

## 测试方法


* 将散图拷贝到 Design/

    ![image](https://github.com/user-attachments/assets/6a87bd49-0d7f-43f9-b03f-97dc6ae2720b)



* 将散图导入到Unity

    ![image](https://github.com/user-attachments/assets/cf94acc6-bab4-45cd-bd57-31619d62f8a4)


* 打开 Assets/Scenes/SampleScene.unity

    ![image](https://github.com/user-attachments/assets/fe64ac3f-3a10-4224-a2c4-eba1c3a3338b)


### Editor

* 找到RawImage、Image或者SpriteRenderer，然后修改DynamicSprite。

    ![image](https://github.com/user-attachments/assets/6350468d-9546-421f-9cc8-14be80da9e81)

* 可以在RawImage的Texture中查看图集纹理

    ![image](https://github.com/user-attachments/assets/fbe0fc46-c194-4949-8d69-3f523971b4bb)

### Runtime

* 启动游戏，控制Dropdown

    ![image](https://github.com/user-attachments/assets/6020641f-0767-49b1-924b-8ad92e5005c3)

* 可以在FrameDebugger中查看图集纹理

    ![image](https://github.com/user-attachments/assets/45beb0e2-3194-4683-858e-212480a83b10)


