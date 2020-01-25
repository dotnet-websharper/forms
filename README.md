# WebSharper.Forms

Forms are a functional, composable, and type-safe form abstraction for building reactive user interfaces in WebSharper,
similar to Formlets but with fine control over the structure of the output.

* [Tutorial][intro] - check here first to learn about Forms
* Demos
  * The [Pets example](http://try.websharper.com/snippet/Dark_Clark/0000Cy) from the tutorial on [Try WebSharper](https://try.websharper.com)
  * The main [test project](https://github.com/dotnet-websharper/forms/tree/master/WebSharper.Forms.Tests) in this repository - check here for inline HTML and *templated* forms
* [License][license] (Apache v2)
* GitHub - [sources][gh], [tracker][issues]
* Community
  * [WebSharper on Gitter][gitter] - technical chat
  * [WebSharper Forums][wsforums] - Got a question?
  * [#websharper on freenode][chat]
* [Need support?][contact] - IntelliFactory

## Wait, formlets and piglets? - I am confused

`WebSharper.Forms` (this project, aka. **reactive** piglets or `WebSharper.UI.Piglets`) is a reactive implementation of the original [WebSharper.Piglets](https://github.com/dotnet-websharper/piglets) library, using [WebSharper.UI](https://github.com/dotnet-websharper/ui), [WebSharper](https://websharper.com)'s main reactive library.

Piglets are a novel UI abstraction pioneered by WebSharper, and are first documented in this IntelliFactory research paper:

> Loic Denuziere, Ernesto Rodriguez, Adam Granicz. **Piglets to the Rescue: Declarative User Interface Specification with Pluggable View Models**. In Symposium on Implementation and Application of Functional Languages (IFL), Nijmegen, The Netherlands, 2013. [ACM](https://dl.acm.org/citation.cfm?id=2620689), **[PDF](http://www.cs.ru.nl/P.Achten/IFL2013/symposium_proceedings_IFL2013/ifl2013_submission_29.pdf)**.

Formlets have similarly been published in academia, among others in [this 2007 draft paper](https://www.cl.cam.ac.uk/~jdy22/papers/idioms-guide.pdf) by Ezra Cooper, Sam Lindley, Philip Wadler, and Jeremy Yallop at the University of Edinburgh.

Formlets have first been ported to F# for WebSharper in 2009, enhanced for dependent flowlets and published in this IntelliFactory research paper:

> Joel Bjornson, Anton Tayanovskyy, Adam Granicz. **Composing Reactive GUIs in F# Using WebSharper**. In Symposium on Implementation and Application of Functional Languages (IFL), Alphen aan den Rijn, The Netherlands, 2010. pp. 203-216. [Springer](https://link.springer.com/chapter/10.1007/978-3-642-24276-2_13)

This early formlet library is available as [WebSharper.Formlets](https://github.com/dotnet-websharper/formlets), and a `WebSharper.UI`-based re-implementation is available as [WebSharper.UI.Formlets](https://github.com/dotnet-websharper/ui.formlets).

Given that reactive forms/piglets are more flexible than formlets, we recommend that you use `WebSharper.Forms` (this project) in your applications.


[chat]: http://webchat.freenode.net/?channels=#websharper
[contact]: http://intellifactory.com/contact
[wsforums]: https://forums.websharper.com/
[fsharp]: http://fsharp.org
[gh]: http://github.com/intellifactory/websharper.forms
[gitter]: https://gitter.im/intellifactory/websharper
[intro]: http://github.com/intellifactory/websharper.forms/blob/master/docs/Introduction.md
[issues]: http://github.com/intellifactory/websharper.forms/issues
[license]: http://github.com/intellifactory/websharper.forms/blob/master/LICENSE.md
[nuget]: http://nuget.org
