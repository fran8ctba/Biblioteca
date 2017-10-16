package edu.ua.caps.binder.Webservices;

import com.android.volley.Response;

import java.util.ArrayList;
import java.util.HashMap;

public class TestData implements DataInterface {

    private static TestData instance;

    public static TestData getInstance(){
        if(instance != null){
            return instance;
        }
        else{
            return sync();
        }

    }
    private static TestData sync(){

        return new TestData();
    }

    <#TestDataMethods#>
}
