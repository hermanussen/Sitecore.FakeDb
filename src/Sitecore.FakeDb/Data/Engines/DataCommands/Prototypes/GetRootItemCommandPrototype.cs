namespace Sitecore.FakeDb.Data.Engines.DataCommands.Prototypes
{
  using System;
  using Sitecore.Data.Items;

  public class GetRootItemCommandPrototype : Sitecore.Data.Engines.DataCommands.GetRootItemCommand
  {
    private readonly DataEngineCommand innerCommand = new DataEngineCommand();

    protected override Sitecore.Data.Engines.DataCommands.GetRootItemCommand CreateInstance()
    {
      return new GetRootItemCommand(this.innerCommand.DataStorage);
    }

    protected override Item DoExecute()
    {
      throw new NotSupportedException();
    }
  }
}