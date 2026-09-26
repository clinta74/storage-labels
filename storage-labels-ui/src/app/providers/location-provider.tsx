import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useParams } from 'react-router';
import { useApi } from '../../api';
import { useAlertMessage } from './alert-provider';

interface LocationContextType {
    location: StorageLocation | null;
    loading: boolean;
    refreshLocation: () => void;
}

const LocationContext = createContext<LocationContextType | undefined>(undefined);

export const LocationProvider: React.FC<{ children: ReactNode }> = ({ children }) => {
    const params = useParams();
    const { Api } = useApi();
    const alert = useAlertMessage();
    // The fetched location is kept with the id it was fetched for, so switching ids derives
    // loading/cleared state instead of setting it from the effect. A null location means the fetch failed.
    const [fetched, setFetched] = useState<{ locationId: number; location: StorageLocation | null }>();
    const [refreshing, setRefreshing] = useState(false);

    const parsedId = params.locationId ? Number(params.locationId) : NaN;
    const locationId = parsedId && !isNaN(parsedId) ? parsedId : null;

    const location = locationId !== null && fetched?.locationId === locationId ? fetched.location : null;
    const loading = refreshing || (locationId !== null && fetched?.locationId !== locationId);

    const fetchLocation = (id: number, isCurrent: () => boolean = () => true) =>
        Api.Location.getLocation(id)
            .then(({ data }) => {
                if (isCurrent()) setFetched({ locationId: id, location: data });
            })
            .catch((error) => {
                if (!isCurrent()) return;
                alert.addMessage(error);
                // A failed refresh keeps what's already loaded
                setFetched(prev => prev?.locationId === id ? prev : { locationId: id, location: null });
            });

    useEffect(() => {
        if (locationId === null) return;

        let current = true;
        fetchLocation(locationId, () => current);
        return () => {
            current = false;
        };
    }, [locationId]);

    const refreshLocation = () => {
        if (locationId !== null) {
            setRefreshing(true);
            fetchLocation(locationId).finally(() => setRefreshing(false));
        }
    };

    return (
        <LocationContext.Provider value={{ location, loading, refreshLocation }}>
            {children}
        </LocationContext.Provider>
    );
};

export const useLocation = () => {
    const context = useContext(LocationContext);
    if (context === undefined) {
        throw new Error('useLocation must be used within a LocationProvider');
    }
    return context;
};
