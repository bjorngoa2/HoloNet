# Arrays

Source: https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/arrays

---

# Arrays (.NET Framework design guidelines)

Note

This content is reprinted by permission of Pearson Education, Inc. from *Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries, 2nd Edition*. That edition was published in 2008, and the book has since been fully revised in the [third edition](https://www.informit.com/store/framework-design-guidelines-conventions-idioms-and-9780135896464). Some of the information on this page may be out-of-date.

✔️ DO prefer using collections over arrays in public APIs. The [Collections](guidelines-for-collections) section provides details about how to choose between collections and arrays.

❌ DO NOT use read-only array fields. The field itself is read-only and can't be changed, but elements in the array can be changed.

✔️ CONSIDER using jagged arrays instead of multidimensional arrays.

A jagged array is an array with elements that are also arrays. The arrays that make up the elements can be of different sizes, leading to less wasted space for some sets of data (e.g., sparse matrix) compared to multidimensional arrays. Furthermore, the CLR optimizes index operations on jagged arrays, so they might exhibit better runtime performance in some scenarios.

*Portions © 2005, 2009 Microsoft Corporation. All rights reserved.*

*Reprinted by permission of Pearson Education, Inc. from [Framework Design Guidelines: Conventions, Idioms, and Patterns for Reusable .NET Libraries, 2nd Edition](https://www.informit.com/store/framework-design-guidelines-conventions-idioms-and-9780321545619) by Krzysztof Cwalina and Brad Abrams, published Oct 22, 2008 by Addison-Wesley Professional as part of the Microsoft Windows Development Series.*

## See also

* [Array](/en-us/dotnet/api/system.array)
* [Framework Design Guidelines](./)
* [Usage Guidelines](usage-guidelines)
