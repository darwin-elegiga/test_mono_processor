using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using static VPay.Ols.Processor.Data.Paging.Defaults;

namespace VPay.Ols.Processor.Data.Paging;

public sealed class PagedResult<TItem> : IEnumerable<TItem>
{
    public IEnumerable<TItem> Items { get; }

    public int CurrentPage { get; }

    public int PageSize { get; }

    public int TotalRows { get; }

    public int TotalPages { get; }

    public PagedResult(IEnumerable<TItem> items, int currentPage, int pageSize, int totalRows)
    {
        if (currentPage <= 0)
            throw new ArgumentOutOfRangeException(nameof(currentPage), currentPage, nameof(currentPage) + " must be greater than zero");
        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), pageSize, nameof(pageSize) + " must be greater than zero");
        if (totalRows < 0)
            throw new ArgumentOutOfRangeException(nameof(totalRows), totalRows, nameof(totalRows) + " must be greater than or equal to zero");

        Items = items;
        CurrentPage = currentPage;
        PageSize = pageSize;
        TotalRows = totalRows;
        TotalPages = (int)Math.Ceiling(TotalRows / (double)PageSize);
    }

    internal PagedResult(IEnumerable<TItem> items, PageModel pageModel) :
        this(items, pageModel.CurrentPage, pageModel.PageSize, pageModel.TotalRows)
    { }

    public IEnumerator<TItem> GetEnumerator() => Items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static PagedResult<TItem> Empty()
    {
        IEnumerable<TItem> items = Enumerable.Empty<TItem>();

        return new PagedResult<TItem>(items, DefaultPage, DefaultPageSize, 0);
    }
}
