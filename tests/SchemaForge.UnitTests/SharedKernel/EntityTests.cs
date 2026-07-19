using FluentAssertions;
using SchemaForge.SharedKernel;

namespace SchemaForge.UnitTests.SharedKernel;

public class EntityTests
{
    private sealed class TestEntity(Guid id) : Entity<Guid>(id);

    private sealed class OtherEntity(Guid id) : Entity<Guid>(id);

    [Fact]
    public void Entities_with_same_id_and_type_are_equal()
    {
        var id = Guid.NewGuid();

        var first = new TestEntity(id);
        var second = new TestEntity(id);

        first.Should().Be(second);
        (first == second).Should().BeTrue();
    }

    [Fact]
    public void Entities_with_different_ids_are_not_equal()
    {
        var first = new TestEntity(Guid.NewGuid());
        var second = new TestEntity(Guid.NewGuid());

        first.Should().NotBe(second);
    }

    [Fact]
    public void Entities_of_different_types_with_the_same_id_are_not_equal()
    {
        var id = Guid.NewGuid();

        Entity<Guid> first = new TestEntity(id);
        Entity<Guid> second = new OtherEntity(id);

        first.Should().NotBe(second);
    }

    [Fact]
    public void Transient_entities_are_never_equal_even_to_themselves_by_id()
    {
        var first = new TestEntity(Guid.Empty);
        var second = new TestEntity(Guid.Empty);

        first.Should().NotBe(second);
    }
}
