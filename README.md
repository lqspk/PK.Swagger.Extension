# PK.Swagger.Extension
Swagger接口导出到MarkDown文档

使用方法：

```C#
GlobalConfiguration.Configuration
.EnableSwaggerExport()
.EnableSwaggerUi(c =>
                    {
                        c.InjectExportScript();
                    });
```                    


导出方法：
项目运行后，访问swagger页面，有一个“导出到markdown”按钮，点击即可。
