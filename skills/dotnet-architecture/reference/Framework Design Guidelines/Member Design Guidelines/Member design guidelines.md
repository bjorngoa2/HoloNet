# Member design guidelines

Source: https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/member

---

# Member Design Guidelines

Methods, properties, events, constructors, and fields are collectively referred to as members. Members are ultimately the means by which framework functionality is exposed to the end users of a framework.

Members can be virtual or nonvirtual, concrete or abstract, static or instance, and can have several different scopes of accessibility. All this variety provides incredible expressiveness but at the same time requires care on the part of the framework designer.

This chapter offers basic guidelines that should be followed when designing members of any type.

## In This Section

[Member Overloading](member-overloading)  
[Property Design](property)  
[Constructor Design](constructor)  
[Event Design](event)  
[Field Design](field)  
[Extension Methods](extension-methods)  
[Operator Overloads](operator-overloads)  
[Parameter Design](parameter-design)  
*Portions © 2005, 2009 Microsoft Corporation. All rights reserved.*

*Reprinted by permission of Pearson Education, Inc. from [Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries, 2nd Edition](https://www.informit.com/store/framework-design-guidelines-conventions-idioms-and-9780321545619) by Krzysztof Cwalina and Brad Abrams, published Oct 22, 2008 by Addison-Wesley Professional as part of the Microsoft Windows Development Series.*

## See also

* [Framework Design Guidelines](./)
