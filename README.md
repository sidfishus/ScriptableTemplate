# ScriptableTemplate

I have used this for creating classic ASP web pages which follow the same general structure and contain a lot of repeated code by deriving from the 'Template' class and using templates for the web page and controls. The resulting .asp files are produced via a C# class which contains the page specific values and parameters. The benefits of this are the removal of repeated code (#1 rule of pragmatic programming...), an increase in productivity, as well as the ability to make wholesale changes to an entire application by changing the intermediary templates that the application is produced from.

I have also used this for generating statically parameterisable SQL stored procedures and views which share the same concepts and repeated code that have the performance benefits of inline code as opposed to having the repeated code in sub-SQL functions.
