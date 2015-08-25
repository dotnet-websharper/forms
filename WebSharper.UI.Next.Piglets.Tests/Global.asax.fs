namespace WebSharper.UI.Next.Piglets.Tests

type Global() =
    inherit System.Web.HttpApplication()

    member g.Application_Start(sender: obj, args: System.EventArgs) =
        ()
