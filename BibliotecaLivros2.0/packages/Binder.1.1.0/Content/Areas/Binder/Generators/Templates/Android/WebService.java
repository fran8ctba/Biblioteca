package edu.ua.caps.binder.Webservices;

import android.app.DownloadManager;
import android.content.Context;


import com.android.volley.AuthFailureError;
import com.android.volley.Request;
import com.android.volley.RequestQueue;
import com.android.volley.Response;
import com.android.volley.VolleyError;
import com.android.volley.toolbox.JsonArrayRequest;
import com.android.volley.toolbox.JsonObjectRequest;
import com.android.volley.toolbox.JsonRequest;
import com.android.volley.toolbox.StringRequest;
import com.android.volley.toolbox.Volley;
import com.google.gson.Gson;
import com.google.gson.reflect.TypeToken;

import org.json.JSONArray;
import org.json.JSONException;
import org.json.JSONObject;

import java.lang.reflect.Type;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.Map;

public class WebService implements DataInterface {
    private static WebService instance;
    final Gson gson = new Gson();
    public static String serviceURL = <#WebServiceAddress#>
    private RequestQueue queue;

    public static WebService getInstance(Context context){
        if(instance != null){

            return instance;
        }
        else{
            instance = sync(context);
            return instance;
        }
    }

    private static WebService sync(Context context){
        WebService newInstance = new WebService(context);
        return newInstance;
    }
    private WebService(Context context){
        this.queue = Volley.newRequestQueue(context);
    }


   <#WebServiceMethods#>



   //****************************************************
   //*** Binder Support Methods *************************
   //****************************************************

   public class JsonBinderRequest<T> {
        Request volleyRequest;

        public JsonBinderRequest(int method, String requestUrl, Object data, final Type responseType, RequestQueue queue, final Response.Listener<T> listener, Response.ErrorListener errorListener) {
            this.jsonRequest(method, requestUrl, data, responseType, queue, listener, errorListener);
        }

        private void jsonRequest(int method, String requestUrl, Object data, final Type responseType, RequestQueue queue, final Response.Listener<T> listener, Response.ErrorListener errorListener) {

            //Create request object, if applicable
            JSONObject requestObject = null;
            if (data != null) {
                try {
                    requestObject = new JSONObject(new Gson().toJson(data));
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }

            //Set full url
            requestUrl = serviceURL + requestUrl;

			//Create request
            Request request = null;

			//Redirect call for list or object
			String responseTypeString = responseType.toString();
            if (responseTypeString.contains("java.util.ArrayList<") || responseTypeString.contains("java.util.List<")) {
				request = new JsonArrayRequest(requestUrl, new Response.Listener<JSONArray>() {
                    @Override
                    public void onResponse(JSONArray response) {
                        T parsedList = gson.fromJson(response.toString(), responseType);
                        listener.onResponse(parsedList);
                    }
                }, errorListener) {
                @Override
                public Map<String, String> getHeaders() throws AuthFailureError {
                    return webserviceHeaders();
                }
            };
			}
			else {
				request = new JsonObjectRequest(method, requestUrl, requestObject, new Response.Listener<JSONObject>() {

                @Override
                public void onResponse(JSONObject response) {
                    //Parse json into object
                    T parsedObject = gson.fromJson(response.toString(), responseType);
                    listener.onResponse(parsedObject);
                }


            }, errorListener) {
                @Override
                public Map<String, String> getHeaders() throws AuthFailureError {
                    return webserviceHeaders();
                }
            };
			}

            //Set tag
            request.setTag(requestUrl);

            //Add request to queue
            queue.add(request);
            this.volleyRequest = request;
        }
    }

    public class BooleanBinderRequest {
        public BooleanBinderRequest(int method, String requestUrl, Object data, RequestQueue queue, final Response.Listener<Boolean> listener, Response.ErrorListener errorListener) {
            this.booleanRequest(method, requestUrl, data, queue, listener, errorListener);
        }

        private void booleanRequest(int method, String requestUrl, Object data, RequestQueue queue, final Response.Listener<Boolean> listener, Response.ErrorListener errorListener) {
            //Set full url
            requestUrl = serviceURL + requestUrl;
            //Create request object, if applicable
            JSONObject requestObject = null;
            if (data != null) {
                try {
                    requestObject = new JSONObject(new Gson().toJson(data));
                } catch (JSONException e) {
                    e.printStackTrace();
                }
            }

            JsonObjectRequest request = new JsonObjectRequest(method, requestUrl, requestObject, new Response.Listener<JSONObject>() {

                @Override
                public void onResponse(JSONObject response) {
                    listener.onResponse(true);
                }

            }, errorListener) {
                @Override
                public Map<String, String> getHeaders() throws AuthFailureError {
                    return webserviceHeaders();
                }
            };

            //Set tag
            request.setTag("booleanRequest");

            //Add request to queue
            queue.add(request);
        }
    }

    private HashMap<String,String> webserviceHeaders(){
        HashMap<String, String> headers = new HashMap<String, String>();
        headers.put("Authorization", "<#AuthorizationToken#>");
        return headers;
    }
}
