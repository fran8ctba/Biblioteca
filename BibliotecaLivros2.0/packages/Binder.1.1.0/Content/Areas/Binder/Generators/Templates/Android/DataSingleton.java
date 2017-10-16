package edu.ua.caps.binder.Data;

import android.content.Context;

import com.android.volley.Response;

import org.json.JSONObject;

import java.util.ArrayList;

public class DataSingleton implements DataInterface {

    private static DataSingleton mSharedInstance = new DataSingleton();
    private DataSources dataSourceType;
    private DataInterface dataSource;

    //Singleton
    public static DataSingleton sharedInstance() {
        return mSharedInstance;
    }

    private DataSingleton() {
        super();
        this.dataSource = dataSourceForType(DataSources.TestData, null);
    }

    //Helper methods
    private DataInterface dataSourceForType(DataSources dataSourceType, Context context) {
        switch (dataSourceType) {
            case TestData:
                return TestData.getInstance();
            case Webservice:
                return WebService.getInstance(context);
            default:
                return null;
        }
    }

    //Context Switcher
    public void setNewDataSource(DataSources source, Context context) {
        this.dataSourceType = source;
        this.dataSource = dataSourceForType(source, context);
    }

   <#DataSingletonMethods#>
}
