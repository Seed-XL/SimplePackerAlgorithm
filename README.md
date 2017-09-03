# SimplePackerAlgorithm
一种非常简单的Pack Sprite的算法实现



##   来源：

之前在找一些打图集的算法时，偶尔在一个网站看到一个非常简单，而且容易实现的打图集算法：[Packing Lightmaps](http://blackpawn.com/texts/lightmaps/default.html)。具体思路大概就是，每次填充一个**Sprite**的时候，就将当前这个可用空间划分为完全适配这个Sprite大小空间(长和宽分别适配)，和剩下的空间，最后形成一颗树状结着的空间切分数据结构。根结点就是整个可用空间的大小，叶子结点要么是已经分给已经Packer的Sprite，要么就是已经可用的空间，可以进行分配和划分。于是自己实现了一个简单的C#的版本，网上还有一个[Cpp的版本](https://github.com/TeamHypersomnia/rectpack2D)，写得很周全。



碎图：

![Raw](http://wx3.sinaimg.cn/mw690/6b98bc8agy1fj6mjrlvbvj20v00jajtz.jpg)

打图集后：

![Pack](http://wx4.sinaimg.cn/mw690/6b98bc8agy1fj6mjqw906j21cq0se13g.jpg)



使用：

![Use](http://wx3.sinaimg.cn/mw690/6b98bc8agy1fj6mju9s2kj21hc0smdol.jpg)