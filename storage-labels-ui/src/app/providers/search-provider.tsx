import React, { createContext, useContext, useState, ReactNode } from 'react';

interface SearchContextType {
    searchQuery: string;
    setSearchQuery: (query: string) => void;
    clearSearch: () => void;
    // v2 pagination state
    currentPage: number;
    pageSize: number;
    totalResults: number;
    totalPages: number;
    setCurrentPage: (page: number) => void;
    setPageSize: (size: number) => void;
    setPaginationInfo: (totalResults: number, totalPages: number) => void;
    // Infinite scroll state
    accumulatedResults: SearchResultResponse[];
    appendResults: (newResults: SearchResultResponse[]) => void;
    resetAccumulatedResults: () => void;
}

const SearchContext = createContext<SearchContextType | undefined>(undefined);

export const SearchProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [searchQuery, setSearchQuery] = useState('');
    const [currentPage, setCurrentPage] = useState(1);
    const [pageSize, setPageSize] = useState(10); // Changed from 20 to 10 for infinite scroll
    const [totalResults, setTotalResults] = useState(0);
    const [totalPages, setTotalPages] = useState(0);
    const [accumulatedResults, setAccumulatedResults] = useState<SearchResultResponse[]>([]);

    const clearSearch = () => {
        setSearchQuery('');
        setCurrentPage(1);
        setTotalResults(0);
        setTotalPages(0);
        setAccumulatedResults([]);
    };

    const setPaginationInfo = (newTotalResults: number, newTotalPages: number) => {
        setTotalResults(newTotalResults);
        setTotalPages(newTotalPages);
    };

    const appendResults = (newResults: SearchResultResponse[]) => {
        setAccumulatedResults((prev) => [...prev, ...newResults]);
    };

    const resetAccumulatedResults = () => {
        setAccumulatedResults([]);
        setCurrentPage(1);
    };

    return (
        <SearchContext.Provider value={{ 
            searchQuery, 
            setSearchQuery, 
            clearSearch,
            currentPage,
            pageSize,
            totalResults,
            totalPages,
            setCurrentPage,
            setPageSize,
            setPaginationInfo,
            accumulatedResults,
            appendResults,
            resetAccumulatedResults
        }}>
            {children}
        </SearchContext.Provider>
    );
};

export const useSearch = () => {
    const context = useContext(SearchContext);
    if (context === undefined) {
        throw new Error('useSearch must be used within a SearchProvider');
    }
    return context;
};
