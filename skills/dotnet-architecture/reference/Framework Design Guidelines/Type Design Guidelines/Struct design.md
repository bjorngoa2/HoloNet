# Struct design

Source: https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/struct

---

# Struct Design

Note

This content is reprinted by permission of Pearson Education, Inc. from *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries, 2nd Edition*. That edition was published in 2008, and the book has since been fully revised in the [third edition](https://www.informit.com/store/framework-design-guidelines-conventions-idioms-and-9780135896464). Some of the information on this page may be out-of-date.

The general-purpose value type is most often referred to as a struct, its C# keyword. This section provides guidelines for general struct design.

❌ DO NOT provide a parameterless constructor for a struct.

Following this guideline allows arrays of structs to be created without having to run the constructor on each item of the array. Notice that C# does not allow structs to have parameterless constructors.

❌ DO NOT define mutable value types.

Mutable value types have several problems. For example, when a property getter returns a value type, the caller receives a copy. Because the copy is created implicitly, developers might not be aware that they are mutating the copy, and not the original value. Also, some languages (dynamic languages, in particular) have problems using mutable value types because even local variables, when dereferenced, cause a copy to be made.

✔️ DO ensure that a state where all instance data is set to zero, false, or null (as appropriate) is valid.

This prevents accidental creation of invalid instances when an array of the structs is created.

✔️ DO implement [IEquatable<T>](/en-us/dotnet/api/system.iequatable-1) on value types.

The [Object.Equals](/en-us/dotnet/api/system.object.equals) method on value types causes boxing, and its default implementation is not very efficient, because it uses reflection. [Equals](/en-us/dotnet/api/system.iequatable-1.equals) can have much better performance and can be implemented so that it will not cause boxing.

❌ DO NOT explicitly extend [ValueType](/en-us/dotnet/api/system.valuetype). In fact, most languages prevent this.

In general, structs can be very useful but should only be used for small, single, immutable values that will not be boxed frequently.

*Portions © 2005, 2009 Microsoft Corporation. All rights reserved.*

*Reprinted by permission of Pearson Education, Inc. from [Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries, 2nd Edition](https://www.informit.com/store/framework-design-guidelines-conventions-idioms-and-9780321545619) by Krzysztof Cwalina and Brad Abrams, published Oct 22, 2008 by Addison-Wesley Professional as part of the Microsoft Windows Development Series.*

## See also

* [Type Design Guidelines](type)
* [Framework Design Guidelines](./)
* [Choosing Between Class and Struct](choosing-between-class-and-struct)
