[![Build Status](https://travis-ci.org/VitalyBrusentsev/TypeScriptGenerators.svg?branch=master)](https://travis-ci.org/VitalyBrusentsev/TypeScriptGenerators)
### What is this project about?
This project is intended to make a versatile generator for making AngularJS/TypeScript definitions from 
ASP.NET WebAPI server endpoints. Think of [DefinitelyTyped](https://github.com/borisyankov/DefinitelyTyped), 
but for your server API.

### How should I use it?
Build the project and use ConsoleGen.exe to scan your .NET assemblies and create a TypeScript definition file.

### Can I use it without AngularJS?
The current generator version is AngularJS-specific, but there are plans to provide 
jQuery-flavoured definitions that would work with `$.ajax` calls instead of the `$http` Angular service.
If you want to use it with another favourite JavaScript UI framework, 
you just need to create a generator class that would  adopt TypeScript generation to your needs.
Use existing [AngularGenerator]
(https://github.com/VitalyBrusentsev/TypeScriptGenerators/blob/master/ConsoleGen/AngularGenerator.cs) as an inspiration.

### Supported .NET code entities
The following .NET code components can be translated into TypeScript definitions:

#### Controllers
Most of WebAPI controllers created for AngularJS consumption 
can be represented in TypeScript.
The following controller written in .NET
```csharp
namespace ShoppingApp.Api
{
  public class OrderController : ApiController
  {
    // ...
    public OrderModel Get(long id)
    {
      return _service.Get(id);
    }

    public long Post([FromBody] OrderModel order)
    {
      return _service.Add(order);
    }        
  }
}
```
Will become the following TypeScript definition:
```typescript
module ShoppingApp.Api {
  export interface IOrderService {
    Get(id: number): ng.IHttpPromise<ShoppingApp.Contracts.IOrderModel>;
    Post(order: ShoppingApp.Contracts.IOrderModel): ng.IHttpPromise<number>;
  }
}
```
As you can see, the code follows AngularJS concepts: 
- A "controller" becomes an AngularJS "service" used for interaction with the server,
- The returned types are wrapped in AngularJS promises.
 
The generated models become TypeScript interfaces, allowing you to strongly type the API consumption code.

*Note*: There is currently no service implementation generated, it is considered to be implemented in a future release. The main concern here is support for various routing schemes.

#### Models (or POCOs)
The models used as parameters or return types of WebAPI controllers are generated into TypeScript interfaces. Supported property types include:
- Numeric types (integral types, floating point types and decimals)
- Boolean
- String
- DateTime (becomes `string` in TypeScript)
- Other POCO models defined in the scanned assemblies
- .NET Enums defined in the scanned assemblies
- Arrays/Enumerables/Lists of the above.

If the generator cannot handle something unusual, it will declare a property of type `any`.

C# model example:
```csharp
namespace ShoppingApp.Contracts
{
  public class OrderModel
  {
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public IEnumerable<OrderDetail> Details { get; set; }
    public decimal Amount { get; set; }
    public List<string> Tags { get; set; }
    public System.Text.StringBuilder UnrelatedProperty { get; set; }
  }
}
```
The generated TypeScript model:
```typescript
module ShoppingApp.Contracts {
  export interface IOrderModel {
    Id: number;
    Date: string;
    Details: ShoppingApp.Contracts.IOrderDetail[];
    Tags: string[];
    UnrelatedProperty: any;
  }
}
```
The IOrderDetails interface will also be generated, provided it was implemented in the scanned assemblies. Otherwise the type of `Details` property will be `any`.

#### Enumerations
The .NET enums referenced in your controllers (or models referenced in your controllers) 
will be generated as TypeScript enums.

### Command Line
To run the generator, run ConsoleGen.exe with the following parameters:

`ConsoleGen assembly1.dll [..assemblyN.dll] OutputFile.ts`

The idea is that you specify any (but at least one) number of .NET assemblies to scan, 
and pass the name of the .ts file to generate as the last parameter.

### Contributing
Feel free to fork the project and adapt it for your use. 
Please consider offering some of the improvements in a form of pull requests 
if you feel the community would also benefit from them.
