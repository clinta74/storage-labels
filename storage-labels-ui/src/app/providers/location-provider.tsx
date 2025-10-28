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
    const [location, setLocation] = useState<StorageLocation | null>(null);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        const locationId = params.locationId ? Number(params.locationId) : NaN;
        
        if (locationId && !isNaN(locationId)) {
            setLoading(true);
            Api.Location.getLocation(locationId)
                .then(({ data }) => {
                    setLocation(data);
                })
                .catch((error) => {
                    alert.addMessage(error);
                })
                .finally(() => {
                    setLoading(false);
                });
        } else {
            // Clear location when no valid locationId
            setLocation(null);
        }
    }, [params.locationId]);

    const refreshLocation = () => {
        const locationId = params.locationId ? Number(params.locationId) : NaN;
        if (locationId && !isNaN(locationId)) {
            setLoading(true);
            Api.Location.getLocation(locationId)
                .then(({ data }) => {
                    setLocation(data);
                })
                .catch((error) => {
                    alert.addMessage(error);
                })
                .finally(() => {
                    setLoading(false);
                });
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
