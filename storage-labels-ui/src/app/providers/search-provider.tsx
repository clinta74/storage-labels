import React, { createContext, useContext, useState, ReactNode } from 'react';

interface SearchContextType {
    searchQuery: string;
    setSearchQuery: (query: string) => void;
    clearSearch: () => void;
}

const SearchContext = createContext<SearchContextType | undefined>(undefined);

export const SearchProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const [searchQuery, setSearchQuery] = useState('');

    const clearSearch = () => {
        setSearchQuery('');
    };

    return (
        <SearchContext.Provider value={{ searchQuery, setSearchQuery, clearSearch }}>
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
